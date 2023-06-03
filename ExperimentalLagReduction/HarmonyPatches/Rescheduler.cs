#define TRACELOG

#if DEBUG
using System.Diagnostics;
#endif

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using AtraBase.Collections;
using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using ExperimentalLagReduction.Framework;
using HarmonyLib;

namespace ExperimentalLagReduction.HarmonyPatches;

/// <summary>
/// Re-does the scheduler so it's faster.
/// </summary>
[HarmonyPatch(typeof(NPC))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:Field names should not begin with underscore", Justification = "Preference.")]
internal static class Rescheduler
{
    private const Gender Ungendered = Gender.Undefined;

    #region fields

    private static readonly ConcurrentDictionary<(string start, string end, Gender gender), List<string>?> PathCache = new();

    private static readonly ThreadLocal<HashSet<string>> _visited = new(static () => new(capacity: 32));

    private static readonly ThreadLocal<Queue<MacroNode>> _queue = new(static () => new(capacity: 32));

#if DEBUG
    private static readonly ThreadLocal<Stopwatch> _stopwatch = new(() => new(), trackAllValues: true);

    private static int cacheHits = 0;
    private static int cacheMisses = 0;

    /// <summary>
    /// Gets the defined watches.
    /// </summary>
    internal static IList<Stopwatch> Watches => _stopwatch.Values;

    /// <summary>
    /// Gets the cache hit ratio.
    /// </summary>
    internal static float CacheHitRatio => (float)cacheHits / (cacheMisses + cacheHits);

    /// <summary>
    /// Gets the total number of cache hits.
    /// </summary>
    internal static int CacheHits => cacheHits;
#endif

    #endregion

    /// <summary>
    /// Gets the number of paths cached.
    /// </summary>
    internal static int CacheCount => PathCache.Count;

    /// <summary>
    /// Given the start, end, and a gender constraint, grab the path from the cache, or null if not found.
    /// </summary>
    /// <param name="start">Start location.</param>
    /// <param name="end">End location.</param>
    /// <param name="gender">Gender constraint, or <see cref="NPC.undefined"/> for not constrained.</param>
    /// <param name="path">The path, if found.</param>
    /// <returns>True if found, false otherwise.</returns>
    internal static bool TryGetPathFromCache(string start, string end, int gender, out List<string>? path)
    {
        static List<string>? ShorterNonNull(List<string>? left, List<string>? right)
        {
            if (left is null)
            {
                return right;
            }
            if (right is null)
            {
                return left;
            }

            return left.Count <= right.Count ? left : right;
        }

        bool foundGeneric = PathCache.TryGetValue((start, end, Ungendered), out path);

        bool foundMale = false;
        if (gender != NPC.female && PathCache.TryGetValue((start, end, Gender.Male), out List<string>? male))
        {
            foundMale = true;
            path = ShorterNonNull(path, male);
        }

        bool foundFemale = false;
        if (gender != NPC.male && PathCache.TryGetValue((start, end, Gender.Female), out List<string>? female))
        {
            foundFemale = true;
            path = ShorterNonNull(path, female);
        }

        return foundGeneric || foundMale || foundFemale;
    }

    /// <summary>
    /// Prints all cache values and also a summary.
    /// </summary>
    internal static void PrintCache()
    {
        Counter<int> counter = new();

        foreach (((string start, string end, Gender gender) key, List<string>? value) in PathCache.OrderBy(static kvp => kvp.Key.start).ThenBy(static kvp => kvp.Value?.Count ?? -1))
        {
            ModEntry.ModMonitor.Log($"( {key.start} -> {key.end} ({key.gender.ToStringFast()})) == " + (value is not null ? string.Join("->", value) + $" [{value.Count}]" : "no path found" ), LogLevel.Info);

            if (value is null)
            {
                counter[0]++;
            }
            else
            {
                counter[value.Count]++;
            }
        }

        ModEntry.ModMonitor.Log($"In total: {PathCache.Count} routes cached for {Game1.locations.Count} locations.", LogLevel.Info);
        foreach ((int key, int value) in counter.OrderBy(static kvp => kvp.Value))
        {
            ModEntry.ModMonitor.Log($"    {value} of length {key}", LogLevel.Info);
        }
    }

    /// <summary>
    /// Calculates the path between one game location to another, getting it from the cache if necessary.
    /// </summary>
    /// <param name="start">Start location.</param>
    /// <param name="end">End location.</param>
    /// <param name="gender">Gender constraints (use <see cref="NPC.undefined"/> for no constraints).</param>
    /// <param name="allowPartialPaths">Whether or not to allow piecing together paths to make a full path. This can make the algo pick a less-optimal path, but it's unlikely and is much faster.</param>
    /// <param name="limit">Search limit.</param>
    /// <returns>Path, or null if not found.</returns>
    internal static List<string>? GetPathFor(GameLocation start, GameLocation? end, Gender gender, bool allowPartialPaths, int limit = int.MaxValue)
    {
        if (limit <= 0)
        {
            ModEntry.ModMonitor.Log($"Cannot call GetPathFor with a limit 0 or lower", LogLevel.Error);
            return null;
        }

        try
        {
            _queue.Value ??= new();
            _queue.Value.Clear();
            _visited.Value ??= new();
            _visited.Value.Clear();

            // seed with initial
            MacroNode startNode = new(start.Name, null, GetGenderConstraint(start.Name));
            _visited.Value.Add(start.Name);

            FindWarpsFrom(startNode, start, _visited.Value, startNode.GenderConstraint, _queue.Value);

            while (_queue.Value.TryDequeue(out MacroNode? node))
            {
                if (Game1.getLocationFromName(node.Name) is not GameLocation current)
                {
                    ModEntry.ModMonitor.LogOnce($"A warp references {node.Name} which could not be found.", LogLevel.Warn);
                    continue;
                }

                // insert into cache
                List<string> route = Unravel(node);
                PathCache.TryAdd((start.Name, node.Name, node.GenderConstraint), route);
                Gender genderConstrainedToCurrentSearch = GetTightestGenderConstraint(gender, node.GenderConstraint);

                if (genderConstrainedToCurrentSearch != Gender.Invalid && end is not null)
                {
                    // found destination, return it.
                    if (end.Name == node.Name)
                    {
                        if (node.GenderConstraint == Ungendered && route.Count > 3)
                        {
                            int total = route.Count;
                            int count = total - 1;
                            do
                            {
                                List<string> segment = route.GetRange(total - count, count);
                                PathCache.TryAdd((segment[0], segment[^1], Ungendered), segment);
                                count--;
                            }
                            while (count > 1);
                        }

                        _visited.Value.Clear();
                        _queue.Value.Clear();
                        return route;
                    }

                    if (allowPartialPaths)
                    {
                        // if we have A->B and B->D, then we can string the path together already.
                        // avoiding trivial one-step stitching because this is more expensive to do.
                        // this isn't technically correct (especially for cycles) but it works pretty well most of the time.
                        if (PathCache.TryGetValue((node.Name, end.Name, Ungendered), out List<string>? prev)
                            && prev?.Count > 2 && CompletelyDistinct(route, prev))
                        {
                            ModEntry.ModMonitor.TraceOnlyLog($"Partial route found: {start.Name} -> {node.Name} + {node.Name} -> {end.Name}", LogLevel.Info);
                            List<string> routeStart = new(route.Count + prev.Count - 1);
                            routeStart.AddRange(route);
                            routeStart.RemoveAt(routeStart.Count - 1);
                            routeStart.AddRange(prev);

                            PathCache.TryAdd((start.Name, end.Name, node.GenderConstraint), routeStart);

                            _visited.Value.Clear();
                            _queue.Value.Clear();
                            return routeStart;
                        }
                        else if (PathCache.TryGetValue((node.Name, end.Name, genderConstrainedToCurrentSearch), out List<string>? genderedPrev)
                            && genderedPrev?.Count > 2 && CompletelyDistinct(route, genderedPrev))
                        {
                            ModEntry.ModMonitor.TraceOnlyLog($"Partial route found: {start.Name} -> {node.Name} + {node.Name} -> {end.Name}", LogLevel.Info);
                            List<string> routeStart = new(route.Count + genderedPrev.Count - 1);
                            routeStart.AddRange(route);
                            routeStart.RemoveAt(routeStart.Count - 1);
                            routeStart.AddRange(genderedPrev);

                            PathCache.TryAdd((start.Name, end.Name, genderConstrainedToCurrentSearch), routeStart);

                            _visited.Value.Clear();
                            _queue.Value.Clear();
                            return routeStart;
                        }
                    }
                }

                if (node.Depth < limit)
                {
                    // queue next
                    FindWarpsFrom(node, current, _visited.Value, node.GenderConstraint, _queue.Value);
                }
            }

            // queue exhausted.
            if (limit == int.MaxValue && end is not null)
            {
                // mark invalid.
                string genderstring = gender switch
                {
                    Gender.Male => "male",
                    Gender.Female => "female",
                    _ => "none",
                };

                ModEntry.ModMonitor.Log($"Scheduler could not find route from {start.Name} to {end.Name} while honoring gender {genderstring}", LogLevel.Warn);
                PathCache.TryAdd((start.Name, end.Name, gender), null);
            }
            return null;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("macropathfinding", ex);
            _visited.Value?.Clear();
            _queue.Value?.Clear();
            return null;
        }
    }

    #region harmony

    [HarmonyPrefix]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPatch(nameof(NPC.populateRoutesFromLocationToLocationList))]
    private static bool PrefixPopulateRoutes()
    {
        try
        {
#if DEBUG
            if (_stopwatch.Values.Count > 0)
            {
                ModEntry.ModMonitor.Log($"Resetting all timers.", LogLevel.Info);
                for (int i = 0; i < _stopwatch.Values.Count; i++)
                {
                    _stopwatch.Values[i] = new();
                }
            }

            ModEntry.ModMonitor.Log((new StackTrace()).ToString());
#endif

            PathCache.Clear();
            PrePopulateCache();

            return false;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("pre-populating pathfinding cache.", ex);
            return true;
        }
    }

    private static void PrePopulateCache()
    {
        if (!ModEntry.Config.PrePopulateCache)
        {
            return;
        }

#if DEBUG
        _stopwatch.Value ??= new();
        _stopwatch.Value.Start();
#endif

        ModEntry.ModMonitor.TraceOnlyLog($"Locations cache contains {Game1._locationLookup.Count} entries.");
        if (Game1.locations.Count > Game1._locationLookup.Count)
        {
            foreach (GameLocation? location in Game1.locations)
            {
                Game1._locationLookup.TryAdd(location.Name, location);
            }
            ModEntry.ModMonitor.TraceOnlyLog($"Locations cache prepopulated with {Game1._locationLookup.Count} entries.");
#if DEBUG
            ModEntry.ModMonitor.Log($"This took {_stopwatch.Value.ElapsedMilliseconds} ms");
#endif
        }

        foreach ((string center, string radius) in AssetManager.GetPrepopulate())
        {
            GameLocation? loc = Game1.getLocationFromName(center);
            if (loc is null)
            {
                ModEntry.ModMonitor.LogOnce($"Could not find location {center} for prepopulating locations for macro scheduler, skipping.", LogLevel.Warn);
                continue;
            }

            if (!int.TryParse(radius, out int limit))
            {
                ModEntry.ModMonitor.LogOnce($"Could not parse radius {radius} for {center}, setting to three.", LogLevel.Warn);
                limit = 3;
            }
            else if (limit < 1 || limit > 4)
            {
                ModEntry.ModMonitor.LogOnce($"Radius {radius} for {center} is out of bounds, clamping.", LogLevel.Warn);
                limit = Math.Clamp(limit, 1, 4);
            }

            Task.Factory.StartNew(() => Prefetch(loc, limit))
            .ContinueWith(
                continuationAction: static (task, _) =>
                {
                    if (task.Status == TaskStatus.Faulted)
                    {
                        ModEntry.ModMonitor.Log($"Cache prepopulation failed. Check log for details.", LogLevel.Error);
                        ModEntry.ModMonitor.Log(task.Exception?.ToString() ?? "no stack trace?");
                    }
                },
                state: null,
                continuationOptions: TaskContinuationOptions.OnlyOnFaulted);
        }

#if DEBUG
        _stopwatch.Value.Stop();
        ModEntry.ModMonitor.Log($"Total time so far: {_stopwatch.Value.ElapsedMilliseconds} ms, {PathCache.Count} total routes cached. Prefetch started.", LogLevel.Info);
#endif
    }

    private static void Prefetch(GameLocation loc, int limit)
    {
#if DEBUG
        _stopwatch.Value ??= new();
        _stopwatch.Value.Start();
#endif

        _ = GetPathFor(loc, null, Ungendered, false, limit);

#if DEBUG
        _stopwatch.Value.Stop();
        ModEntry.ModMonitor.Log($"Prefetch done for {loc.Name}. Total time so far: {_stopwatch.Value.ElapsedMilliseconds} ms, {PathCache.Count} total routes cached", LogLevel.Info);
#endif
    }

    [HarmonyPrefix]
    [HarmonyPatch("getLocationRoute")]
    [HarmonyPriority(Priority.VeryLow)]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1123:Do not place regions within elements", Justification = "Preference.")]
    private static bool PrefixGetLocationRoute(string startingLocation, string endingLocation, NPC __instance, ref List<string>? __result)
    {
        if (TryGetPathFromCache(startingLocation, endingLocation, __instance.Gender, out __result))
        {
#if DEBUG
            Interlocked.Increment(ref cacheHits);
#endif

            ModEntry.ModMonitor.TraceOnlyLog($"Got macro schedule for {__instance.Name} from cache: {startingLocation} -> {endingLocation}");
            if (__result is null)
            {
                ModEntry.ModMonitor.Log($"{__instance.Name} requested path from {startingLocation} to {endingLocation} where no valid path was found.", LogLevel.Warn);
            }
            return false;
        }

        #region validation

        __result = null;
        if (GetActualLocation(endingLocation) is not string actualEnd)
        {
            ModEntry.ModMonitor.Log($"{__instance.Name} requested path to {endingLocation} which is blacklisted from pathing", LogLevel.Warn);
            return false;
        }

        GameLocation start = Game1.getLocationFromName(startingLocation);
        if (start is null)
        {
            ModEntry.ModMonitor.Log($"NPC {__instance.Name} requested path starting at {startingLocation}, which does not exist.", LogLevel.Warn);
            return false;
        }
        Gender startGender = GetTightestGenderConstraint((Gender)__instance.Gender, GetGenderConstraint(startingLocation));
        if (startGender == Gender.Invalid)
        {
            ModEntry.ModMonitor.Log($"NPC {__instance.Name} requested path starting at {startingLocation}, which is not allowed due to their gender.", LogLevel.Warn);
            return false;
        }

        GameLocation end = Game1.getLocationFromName(actualEnd);
        if (end is null)
        {
            ModEntry.ModMonitor.Log($"NPC {__instance.Name} requested path ending at {actualEnd}, which does not exist.", LogLevel.Warn);
            return false;
        }
        Gender endGender = GetTightestGenderConstraint((Gender)__instance.Gender, GetGenderConstraint(actualEnd));
        if (endGender == Gender.Invalid)
        {
            ModEntry.ModMonitor.Log($"NPC {__instance.Name} requested path ending at {actualEnd}, which is not allowed due to their gender.", LogLevel.Warn);
            return false;
        }

        #endregion

#if DEBUG
        _stopwatch.Value ??= new();
        _stopwatch.Value.Start();

        Interlocked.Increment(ref cacheMisses);
#endif
        __result = GetPathFor(start, end, (Gender)__instance.Gender, ModEntry.Config.AllowPartialPaths);

#if DEBUG
        _stopwatch.Value.Stop();
        ModEntry.ModMonitor.Log($"Total time so far: {_stopwatch.Value.ElapsedMilliseconds} ms, {PathCache.Count} total routes cached", LogLevel.Info);
#endif

        if (__result is null)
        {
            ModEntry.ModMonitor.LogOnce($"{__instance.Name} requested path from {startingLocation} to {endingLocation} where no valid path was found.", LogLevel.Warn);
        }
        else
        {
            ModEntry.ModMonitor.TraceOnlyLog($"Found path for {__instance.Name} from {startingLocation} to {endingLocation}: {string.Join("->", __result)} with {__result.Count} segments.");
        }
        return false;
    }

#endregion

    #region helpers

    private static bool CompletelyDistinct(List<string> route, List<string> prev)
    {
        Span<string> first = CollectionsMarshal.AsSpan(prev);
        Span<string> second = CollectionsMarshal.AsSpan(route)[..^1];

        foreach (string? x in first)
        {
            foreach (string? y in second)
            {
                if (x == y)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static List<string> Unravel(MacroNode node)
    {
        List<string> ret = new(node.Depth + 1);
        for (int i = 0; i <= node.Depth; ++i)
        {
            ret.Add(string.Empty);
        }

        MacroNode? workingNode = node;
        do
        {
            ret[workingNode.Depth] = workingNode.Name;
            workingNode = workingNode.Prev;
        }
        while (workingNode is not null);

        return ret;
    }

    /// <summary>
    /// Gets the locations leaving a specific place, keeping in mind the locations already visited.
    /// </summary>
    /// <param name="start">Node to start from.</param>
    /// <param name="location">Location to look at.</param>
    /// <param name="visited">Previous visited locations.</param>
    /// <param name="gender">Current gender constraint for the path.</param>
    /// <param name="queue">Queue to add to.</param>
    private static void FindWarpsFrom(MacroNode start, GameLocation? location, HashSet<string> visited, Gender gender, Queue<MacroNode> queue)
    {
        if (location is null)
        {
            return;
        }

        if (location.warps?.Count is not null and not 0)
        {
            foreach (Warp? warp in location.warps)
            {
                if (GetActualLocation(warp.TargetName) is string name)
                {
                    Gender genderConstraint = GetTightestGenderConstraint(gender, GetGenderConstraint(name));
                    if (genderConstraint != Gender.Invalid && visited.Add(name))
                    {
                        queue.Enqueue(new(name, start, genderConstraint));
                    }
                }
            }
        }

        if (location.doors?.Count() is not 0 and not null)
        {
            foreach (string? door in location.doors.Values)
            {
                if (GetActualLocation(door) is string name)
                {
                    Gender genderConstraint = GetTightestGenderConstraint(gender, GetGenderConstraint(name));
                    if (genderConstraint != Gender.Invalid && visited.Add(name))
                    {
                        queue.Enqueue(new(name, start, genderConstraint));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets the actual location a warp name corresponds to, or null if it should be blacklisted from scheduling.
    /// </summary>
    /// <param name="name">Location to start from.</param>
    /// <returns>The actual location name for a specific location.</returns>
    private static string? GetActualLocation(string name)
    {
        // exclude cellars entirely.
        if (name.StartsWith("Cellar", StringComparison.Ordinal) && long.TryParse(name["Cellar".Length..], out _))
        {
            return null;
        }
        return name switch
        {
            "Farm" or "Woods" or "Backwoods" or "Tunnel" or "Volcano" or "VolcanoEntrance" => null,
            "BoatTunnel" => "IslandSouth",
            _ => name,
        };
    }

    #endregion

    #region gender

    /// <summary>
    /// Given a map, get its gender restrictions.
    /// </summary>
    /// <param name="name">Name of map.</param>
    /// <returns>Gender to restrict to.</returns>
    private static Gender GetGenderConstraint(string name)
        => name switch
        {
            "BathHouse_MensLocker" => Gender.Male,
            "BathHouse_WomensLocker" => Gender.Female,
            _ => Ungendered,
        };

    /// <summary>
    /// Given two gender constraints, return the tighter of the two.
    /// </summary>
    /// <param name="first">First gender constraint.</param>
    /// <param name="second">Second gender constraint.</param>
    /// <returns>Gender constraint, using Gender.Invalid for unsatisfiable.</returns>
    private static Gender GetTightestGenderConstraint(Gender first, Gender second)
    {
        if (first == Ungendered || first == second)
        {
            return second;
        }
        if (second == Ungendered)
        {
            return first;
        }
        return Gender.Invalid;
    }

    #endregion

    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "The entire class is private?")]
    private class MacroNode
    {
        internal readonly string Name;
        internal readonly MacroNode? Prev;
        internal readonly Gender GenderConstraint;
        internal readonly int Depth;

        internal MacroNode(string name, MacroNode? prev, Gender genderConstraint)
        {
            this.Name = name;
            this.Prev = prev;
            this.GenderConstraint = genderConstraint;
            if (prev?.Depth is int previousDepth)
            {
                this.Depth = previousDepth + 1;
            }
            else
            {
                this.Depth = 0;
            }
        }
    }
}
