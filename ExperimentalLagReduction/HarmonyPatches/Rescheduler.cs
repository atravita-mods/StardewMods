#define TIMING
// #define TRACELOG

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using AtraBase.Collections;
using AtraBase.Toolkit;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using ExperimentalLagReduction.Framework;

using HarmonyLib;

using StardewValley.GameData.Characters;
using StardewValley.Locations;
using StardewValley.Pathfinding;

namespace ExperimentalLagReduction.HarmonyPatches;

/// <summary>
/// Re-does the scheduler so it's faster.
/// </summary>
[HarmonyPatch(typeof(WarpPathfindingCache))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:Field names should not begin with underscore", Justification = "Preference.")]
internal static class Rescheduler
{
    private const Gender Ungendered = Gender.Undefined;

    #region fields

    private static readonly ConcurrentDictionary<(string start, string end), PathSet> PathCache = new(capacity: 256, concurrencyLevel: Environment.ProcessorCount);

    private static readonly ThreadLocal<HashSet<string>> _visited = new(static () => new(capacity: 32));

    private static readonly ThreadLocal<Queue<MacroNode>> _queue = new(static () => new(capacity: 32));

    private static readonly ThreadLocal<HashSet<string>> _dedup = new(static () => new(16));

    private static bool preCached = false;

#if TIMING
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

    /// <inheritdoc cref="IExperimentalLagReductionAPI.ClearPathNulls"/>
    internal static bool ClearNulls()
    {
        bool ret = false;
        foreach (((string start, string end) k, PathSet v) in PathCache)
        {
            if (v.MalePath?.Length == 0)
            {
                v.MalePath = null;
                ret = true;
            }

            if (v.FemalePath?.Length == 0)
            {
                v.FemalePath = null;
                ret = true;
            }

            if (v.UndefinedPath?.Length == 0)
            {
                v.UndefinedPath = null;
                ret = true;
            }

            if (v.MalePath is null && v.FemalePath is null && v.UndefinedPath is null)
            {
                PathCache.TryRemove(k, out _);
            }
        }
        return ret;
    }

    /// <inheritdoc cref="IExperimentalLagReductionAPI.ClearMacroCache"/>
    internal static bool ClearCache()
    {
        preCached = false;
        if (PathCache.IsEmpty)
        {
            return false;
        }
        PathCache.Clear();
        return true;
    }

    /// <summary>
    /// Tries to pre-populate the cache if player config allows and the cache has not been previously populated.
    /// </summary>
    /// <param name="parallel">whether or not to try to run off the main thread.</param>
    /// <returns>True if runs, false otherwise. </returns>
    internal static bool PrePopulateCache(bool parallel = true)
    {
        if (!ModEntry.Config.PrePopulateCache || preCached)
        {
            return false;
        }

        preCached = true;

#if TIMING
        _stopwatch.Value ??= new();
        _stopwatch.Value.Start();
#endif

        ModEntry.ModMonitor.TraceOnlyLog($"Locations cache contains {Game1._locationLookup.Count} entries.");
        if (Game1.locations.Count > Game1._locationLookup.Count)
        {
            Game1._locationLookup.EnsureCapacity(Game1.locations.Count + 4);
            foreach (GameLocation? location in Game1.locations)
            {
                Game1._locationLookup.TryAdd(location.Name, location);
            }
#if TIMING
            ModEntry.ModMonitor.TraceOnlyLog($"Locations cache prepopulated with {Game1._locationLookup.Count} entries.");
            ModEntry.ModMonitor.TraceOnlyLog($"This took {_stopwatch.Value.Elapsed.TotalMilliseconds:F2} ms");
#endif
        }

        List<(GameLocation center, int radius)> centers = [];
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

            centers.Add((loc, limit));
        }

        if (parallel)
        {
            new Thread(PrefetchWorker).Start(centers);
        }
        else
        {
            PrefetchWorker(centers);
        }

#if TIMING
        _stopwatch.Value.Stop();
        ModEntry.ModMonitor.Log($"Total time so far: {_stopwatch.Value.Elapsed.TotalMilliseconds:F2} ms, {PathCache.Count} total routes cached. Prefetch started.", LogLevel.Info);
#endif

        return true;
    }

    private static void PrefetchWorker(object? obj)
    {

#if TIMING
        ModEntry.ModMonitor.Log($"Prefetch started on thread {Environment.CurrentManagedThreadId}.", LogLevel.Info);
        _stopwatch.Value ??= new();
        _stopwatch.Value.Start();
#endif

        Stopwatch timer = Stopwatch.StartNew();
        HashSet<string> handled = [];

        try
        {
            List<(GameLocation center, int radius)> work = (List<(GameLocation center, int radius)>)obj!;
            foreach ((GameLocation center, int radius) in work)
            {
                handled.Add(center.Name);
                _ = GetPathFor(center, null, Ungendered, false, radius);

#if TIMING && TRACELOG
                ModEntry.ModMonitor.Log($"Prefetch done for {center.NameOrUniqueName}. Total time: {_stopwatch.Value.Elapsed.TotalMilliseconds:F2} ms, {PathCache.Count} total routes cached.", LogLevel.Info);
#endif
            }

            ModEntry.ModMonitor.TraceOnlyLog($"Okay, finished with data-driven prefetch, now prefetching from NPC locations.");

            IDictionary<string, CharacterData> characterData = Game1.characterData;

            foreach ((string name, CharacterData data) in characterData)
            {
                if (timer.ElapsedMilliseconds > 250)
                {
                    break;
                }

                if (data.Home?.Count is null or 0)
                {
                    continue;
                }

                string home = data.Home[^1].Location;
                if (!handled.Add(home) || Game1.getLocationFromName(home) is not GameLocation homeLocation)
                {
                    continue;
                }

                ModEntry.ModMonitor.TraceOnlyLog($"Prefetching for {name} at {home} - time {timer.ElapsedMilliseconds:F2} with {PathCache.Count}.", LogLevel.Trace);
                _ = GetPathFor(homeLocation, null, Ungendered, false, 2);
            }

#if TIMING
            _stopwatch.Value.Stop();
            ModEntry.ModMonitor.Log($"Prefetch done. Total time: {_stopwatch.Value.Elapsed.TotalMilliseconds:F2} ms, {PathCache.Count} total routes cached.", LogLevel.Info);
#endif
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("pre-populating cache", ex);
        }
    }

    /// <summary>
    /// Given the start, end, and a gender constraint, grab the path from the cache, or null if not found.
    /// </summary>
    /// <param name="start">Start location.</param>
    /// <param name="end">End location.</param>
    /// <param name="gender">Gender constraint, or <see cref="NPC.undefined"/> for not constrained.</param>
    /// <param name="path">The path, if found.</param>
    /// <returns>True if cached, false otherwise.</returns>
    /// <remarks>Returning true with path as null represents an INVALID path.</remarks>
    internal static bool TryGetPathFromCache(string start, string end, int gender, out string[]? path)
    {
        static string[]? ShorterNonNull(string[]? left, string[]? right)
        {
            if (left is null || left.Length == 0)
            {
                return right;
            }
            if (right is null || right.Length == 0)
            {
                return left;
            }

            return left.Length <= right.Length ? left : right;
        }

        path = null;
        if (!PathCache.TryGetValue((start, end), out PathSet? set))
        {
            return false;
        }

        path = set.UndefinedPath;

        if (gender != NPC.female)
        {
            path = ShorterNonNull(path, set.MalePath);
        }

        if (gender != NPC.male)
        {
            path = ShorterNonNull(path, set.FemalePath);
        }

        if (path?.Length == 0)
        {
            path = null;
        }

        return true;
    }

    /// <summary>
    /// Prints all cache values and also a summary.
    /// </summary>
    internal static void PrintCache()
    {
        Counter<int> counter = [];

        foreach (((string start, string end) key, PathSet set) in PathCache.OrderBy(static kvp => kvp.Key.start).ThenBy(static kvp => kvp.Value.MaxLength()))
        {
            if (set.UndefinedPath is { } undefined)
            {
                ModEntry.ModMonitor.Log($"( {key.start} -> {key.end} (Undefined)) == " + (undefined.Length != 0 ? string.Join("->", undefined) + $" [{undefined.Length}]" : "no path found"), LogLevel.Info);
                counter[undefined.Length]++;
            }

            if (set.FemalePath is { } female)
            {
                ModEntry.ModMonitor.Log($"( {key.start} -> {key.end} (Female)) == " + (female.Length != 0 ? string.Join("->", female) + $" [{female.Length}]" : "no path found"), LogLevel.Info);
                counter[female.Length]++;
            }

            if (set.MalePath is { } male)
            {
                ModEntry.ModMonitor.Log($"( {key.start} -> {key.end} (Female)) == " + (male.Length != 0 ? string.Join("->", male) + $" [{male.Length}]" : "no path found"), LogLevel.Info);
                counter[male.Length]++;
            }
        }

        ModEntry.ModMonitor.Log($"In total: {PathCache.Count} routes cached for {Game1.locations.Count} locations.", LogLevel.Info);
        foreach ((int key, int value) in counter)
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
    internal static string[]? GetPathFor(GameLocation start, GameLocation? end, Gender gender, bool allowPartialPaths, int limit = int.MaxValue)
    {
        if (limit <= 0)
        {
            ModEntry.ModMonitor.Log("Cannot call GetPathFor with a limit 0 or lower", LogLevel.Error);
            return null;
        }

        if (start.ShouldExcludeFromNpcPathfinding())
        {
            ModEntry.ModMonitor.Log($"{start.NameOrUniqueName} has been excluded from the pathfinding system.", LogLevel.Warn);
            return null;
        }

        if (end is not null && end.ShouldExcludeFromNpcPathfinding())
        {
            ModEntry.ModMonitor.Log($"{end.NameOrUniqueName} has been excluded from the pathfinding system.", LogLevel.Warn);
            return null;
        }

        try
        {
            _queue.Value ??= new();
            _queue.Value.Clear();
            _visited.Value ??= [];
            _visited.Value.Clear();

            // seed with initial
            MacroNode startNode = new(start.Name, null, GetGenderConstraint(start.Name));
            string[]? ret = null;

            FindWarpsFrom(startNode, start, _visited.Value, startNode.GenderConstraint, _queue.Value);

            while (_queue.Value.TryDequeue(out MacroNode? node))
            {
                _visited.Value.Add(node.Name);
                if (Game1.getLocationFromName(node.Name) is not GameLocation current)
                {
                    ModEntry.ModMonitor.LogOnce($"A warp references {node.Name} which could not be found.", LogLevel.Warn);
                    continue;
                }

                if (current.ShouldExcludeFromNpcPathfinding())
                {
                    ModEntry.ModMonitor.VerboseLog($"Reached map {current.Name} which has been excluded from the macropathfinder.");
                    continue;
                }

                // insert into cache
                string[] route = Unravel(node);
                InsertRoute(start.Name, node.Name, node.GenderConstraint, route);

                if (ret is not null)
                {
                    continue;
                }

                Gender genderConstrainedToCurrentSearch = GetTightestGenderConstraint(gender, node.GenderConstraint);

                if (genderConstrainedToCurrentSearch != Gender.Invalid && end is not null)
                {
                    // found destination, return it.
                    if (end.Name == node.Name)
                    {
                        if (node.GenderConstraint == Ungendered && route.Length > 3)
                        {
                            int index = route.Length - 2;
                            do
                            {
                                if (PathCache.ContainsKey((route[index], route[^1])))
                                {
                                    index--;
                                    continue;
                                }

                                string[] segment = route[index..];
                                ModEntry.ModMonitor.TraceOnlyLog($"Backtrack inserting path from {segment[0]} to {segment[^1]}", LogLevel.Trace);
                                InsertRoute(segment[0], segment[^1], Ungendered, segment);
                                index--;
                            }
                            while (index > 0);
                        }

                        ret = route;
                    }

                    // if we have A->B and B->D, then we can string the path together already.
                    // avoiding trivial one-step stitching because this is more expensive to do.
                    // this isn't technically correct (especially for cycles) but it works pretty well most of the time.
                    else if (allowPartialPaths && PathCache.TryGetValue((node.Name, end.Name), out PathSet? set))
                    {
                        if (set.UndefinedPath is { } prev)
                        {
                            if (prev.Length > 2 && CompletelyDistinct(route, prev))
                            {
                                ModEntry.ModMonitor.TraceOnlyLog($"Partial route found: {start.Name} -> {node.Name} + {node.Name} -> {end.Name}", LogLevel.Info);
                                string[] routeStart = new string[route.Length + prev.Length - 1];
                                Array.Copy(route, routeStart, route.Length - 1);
                                Array.Copy(prev, 0, routeStart, route.Length - 1, prev.Length);

                                InsertRoute(start.Name, end.Name, node.GenderConstraint, routeStart);
                                ret = routeStart;
                            }
                        }
                        else
                        {
                            string[]? genderedPrev = genderConstrainedToCurrentSearch switch
                            {
                                Gender.Male => set.MalePath,
                                Gender.Female => set.FemalePath,
                                _ => null
                            };

                            if (genderedPrev is not null && genderedPrev.Length > 2 && CompletelyDistinct(route, genderedPrev))
                            {
                                ModEntry.ModMonitor.TraceOnlyLog($"Partial route found: {start.Name} -> {node.Name} + {node.Name} -> {end.Name}", LogLevel.Info);
                                string[] routeStart = new string[route.Length + genderedPrev.Length - 1];
                                Array.Copy(route, routeStart, route.Length - 1);
                                Array.Copy(genderedPrev, 0, routeStart, route.Length - 1, genderedPrev.Length);

                                InsertRoute(start.Name, end.Name, genderConstrainedToCurrentSearch, routeStart);
                                ret = routeStart;
                            }
                        }
                    }
                }

                if (node.Depth < limit && ret is null)
                {
                    // queue next
                    FindWarpsFrom(node, current, _visited.Value, node.GenderConstraint, _queue.Value);
                }
            }

            if (ret is not null)
            {
                _visited.Value.Clear();
                _queue.Value.Clear();
                return ret;
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
                InsertRoute(start.Name, end.Name, gender, []);
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

    [MethodImpl(TKConstants.Hot)]
    private static void InsertRoute(string start, string end, Gender constraint, string[] route)
    {
        PathCache.AddOrUpdate(
            (start, end),
            _ => GetNewPathSet(constraint, route),
            (_, prev) => UpdatePathSetFor(constraint, prev, route));
    }

    [MethodImpl(TKConstants.Hot)]
    private static PathSet GetNewPathSet(Gender genderConstraint, string[] route)
        => genderConstraint switch
        {
            Gender.Male => new(route, null, null),
            Gender.Female => new(null, route, null),
            Gender.Undefined => new(null, null, route),
            _ => new(null, null, null),
        };

    [MethodImpl(TKConstants.Hot)]
    private static PathSet UpdatePathSetFor(Gender genderConstraint, PathSet prev, string[] route)
    {
        switch (genderConstraint)
        {
            case Gender.Male:
                prev.MalePath = route;
                break;
            case Gender.Female:
                prev.FemalePath = route;
                break;
            case Gender.Undefined:
                prev.UndefinedPath = route;
                break;
        }

        return prev;
    }

    #region harmony

    [HarmonyPrefix]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPatch(nameof(WarpPathfindingCache.PopulateCache))]
    private static bool PrefixPopulateRoutes()
    {
        try
        {
#if TIMING
            if (_stopwatch.Values.Count > 0)
            {
                ModEntry.ModMonitor.Log($"Resetting all timers.", LogLevel.Info);
                for (int i = 0; i < _stopwatch.Values.Count; i++)
                {
                    _stopwatch.Values[i] = new();
                }
            }

#endif

            ModEntry.ModMonitor.TraceOnlyLog(new StackTrace().ToString(), LogLevel.Info);
            ClearCache();

            for (int i = 1; i <= Game1.netWorldState.Value.HighestPlayerLimit; i++)
            {
                WarpPathfindingCache.IgnoreLocationNames.Add("Cellar" + i);
            }

            // avoid pre-caching if we're in the middle of the day.
            if (Game1.newDay || Game1.gameMode == Game1.loadingMode)
            {
                PrePopulateCache();
            }

            return false;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("pre-populating pathfinding cache.", ex);
            return true;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(WarpPathfindingCache.GetLocationRoute))]
    [HarmonyPriority(Priority.VeryLow)]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1123:Do not place regions within elements", Justification = "Preference.")]
    private static bool PrefixGetLocationRoute(string startingLocation, string endingLocation, int gender, ref string[]? __result)
    {
#if TIMING
        _stopwatch.Value ??= new();
        _stopwatch.Value.Start();
#endif

        bool found = TryGetPathFromCache(startingLocation, endingLocation, gender, out __result);

#if TIMING
        _stopwatch.Value.Stop();
#endif

        if (found)
        {
#if TIMING
            Interlocked.Increment(ref cacheHits);
#endif

            ModEntry.ModMonitor.TraceOnlyLog($"Got macro schedule from cache: {startingLocation} -> {endingLocation}");
            if (__result is null)
            {
                ModEntry.ModMonitor.Log($"Gender {gender} requested path from {startingLocation} to {endingLocation} where no valid path was found.", LogLevel.Warn);
            }
            return false;
        }

        #region validation

        __result = null;
        if (GetActualLocation(startingLocation) is not string actualStart)
        {
            ModEntry.ModMonitor.Log($"Requested path to {endingLocation} is blacklisted from pathing", LogLevel.Warn);
            return false;
        }

        if (GetActualLocation(endingLocation) is not string actualEnd)
        {
            ModEntry.ModMonitor.Log($"Requested path to {endingLocation} is blacklisted from pathing", LogLevel.Warn);
            return false;
        }

        GameLocation start = Game1.getLocationFromName(actualStart);
        if (start is null)
        {
            ModEntry.ModMonitor.Log($"Requested path starting at {startingLocation}, which does not exist.", LogLevel.Warn);
            return false;
        }

        Gender startGender = GetTightestGenderConstraint((Gender)gender, GetGenderConstraint(startingLocation));
        if (startGender == Gender.Invalid)
        {
            ModEntry.ModMonitor.Log($"Requested path starting at {startingLocation}, which is not allowed due to gender constraint {gender}.", LogLevel.Warn);
            return false;
        }

        GameLocation end = Game1.getLocationFromName(actualEnd);
        if (end is null)
        {
            ModEntry.ModMonitor.Log($"Requested path starting at {endingLocation}, which does not exist.", LogLevel.Warn);
            return false;
        }
        Gender endGender = GetTightestGenderConstraint((Gender)gender, GetGenderConstraint(actualEnd));
        if (endGender == Gender.Invalid)
        {
            ModEntry.ModMonitor.Log($"Requested path starting at {endingLocation}, which is not allowed due to gender constraint {gender}.", LogLevel.Warn);
            return false;
        }

        #endregion

#if TIMING
        _stopwatch.Value.Start();
        Interlocked.Increment(ref cacheMisses);
#endif
        __result = GetPathFor(start, end, (Gender)gender, ModEntry.Config.AllowPartialPaths);
        if (__result is null)
        {
            ModEntry.ModMonitor.LogOnce($"Requested path from {startingLocation} to {endingLocation} for gender {gender} where no valid path was found.", LogLevel.Warn);
        }
#if TIMING
        else
        {
            ModEntry.ModMonitor.TraceOnlyLog($"Found path from {startingLocation} to {endingLocation} (gender '{gender}'): {string.Join("->", __result)} with {__result.Length} segments.");
        }

        _stopwatch.Value.Stop();
        ModEntry.ModMonitor.Log($"Total time so far: {_stopwatch.Value.Elapsed.TotalMilliseconds:F2} ms, {PathCache.Count} total routes cached", LogLevel.Info);
#endif

        return false;
    }

    #endregion

    #region helpers

    private static bool CompletelyDistinct(string[] route, string[] prev)
    {
        Span<string> first = new(prev);
        Span<string> second = new Span<string>(route)[..^1];

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

    [MethodImpl(TKConstants.Hot)]
    private static string[] Unravel(MacroNode node)
    {
        string[] ret = new string[node.Depth + 1];

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
    /// <param name="node">Node to start from.</param>
    /// <param name="location">Location to look at.</param>
    /// <param name="visited">Previous visited locations.</param>
    /// <param name="gender">Current gender constraint for the path.</param>
    /// <param name="queue">Queue to add to.</param>
    private static void FindWarpsFrom(MacroNode node, GameLocation? location, HashSet<string> visited, Gender gender, Queue<MacroNode> queue)
    {
        if (location is null)
        {
            return;
        }

        _dedup.Value ??= [];
        _dedup.Value.Clear();
        if (location.warps?.Count is not null and not 0)
        {
            foreach (Warp? warp in location.warps)
            {
                if (GetActualLocation(warp.TargetName) is string name && _dedup.Value.Add(name) && !visited.Contains(name))
                {
                    Gender genderConstraint = GetTightestGenderConstraint(gender, GetGenderConstraint(name));
                    if (genderConstraint != Gender.Invalid)
                    {
                        queue.Enqueue(new(name, node, genderConstraint));
                    }
                }
            }
        }

        if (location.doors?.Count() is not 0 and not null)
        {
            foreach (string? door in location.doors.Values)
            {
                if (GetActualLocation(door) is string name && _dedup.Value.Add(name) && !visited.Contains(name))
                {
                    Gender genderConstraint = GetTightestGenderConstraint(gender, GetGenderConstraint(name));
                    if (genderConstraint != Gender.Invalid)
                    {
                        queue.Enqueue(new(name, node, genderConstraint));
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
        if (WarpPathfindingCache.OverrideTargetNames.TryGetValue(name, out string? target))
        {
            name = target;
        }

        if (WarpPathfindingCache.IgnoreLocationNames.Contains(name))
        {
            return null;
        }
        if (VolcanoDungeon.IsGeneratedLevel(name, out _) || MineShaft.IsGeneratedLevel(name, out _))
        {
            return null;
        }
        return name;
    }

    #endregion

    #region gender

    /// <summary>
    /// Given a map, get its gender restrictions.
    /// </summary>
    /// <param name="name">Name of map.</param>
    /// <returns>Gender to restrict to.</returns>
    private static Gender GetGenderConstraint(string name)
        => WarpPathfindingCache.GenderRestrictions.TryGetValue(name, out int gender) ? (Gender)gender : Ungendered;

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
            this.Depth = prev?.Depth is int previousDepth ? previousDepth + 1 : 0;
        }
    }
}

/// <summary>
/// Represents a set of paths.
/// </summary>
/// <remarks>If a field is set to null, that means we have no info on it. If a field set to Array.Empty, that means there is no valid path.</remarks>
internal class PathSet(string[]? malePath, string[]? femalePath, string[]? undefinedPath)
{
    internal string[]? MalePath { get; set; } = malePath;

    internal string[]? FemalePath { get; set; } = femalePath;

    internal string[]? UndefinedPath { get; set; } = undefinedPath;

    internal int MaxLength()
    {
        int length = this.MalePath?.Length ?? 0;
        if (this.FemalePath is { } female)
        {
            length = Math.Max(female.Length, length);
        }

        if (this.UndefinedPath is { } undefined)
        {
            length = Math.Max(undefined.Length, length);
        }

        return length;
    }
}
