#define TRACELOG

using System.Collections.Concurrent;
using System.Diagnostics;

using AtraBase.Collections;

using AtraShared.Utils.Extensions;

using HarmonyLib;

namespace ExperimentalLagReduction.HarmonyPatches;

/// <summary>
/// Re-does the scheduler so it's faster.
/// </summary>
[HarmonyPatch(typeof(NPC))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention.")]
internal static class Rescheduler
{
    private const int ungendered = NPC.undefined;
    private const int invalid_gender = -2;

    private class MacroNode
    {
        internal readonly string name;
        internal readonly MacroNode? prev;
        internal readonly int genderConstraint;
        internal readonly int depth;

        public MacroNode(string name, MacroNode? prev, int genderConstraint)
        {
            this.name = name;
            this.prev = prev;
            this.genderConstraint = genderConstraint;
            if (prev?.depth is int prevdepth)
            {
                this.depth = prevdepth + 1;
            }
            else
            {
                this.depth = 0;
            }
        }
    }

    private static readonly ConcurrentDictionary<(string start, string end, int gender), List<string>?> pathCache = new();

    private static readonly ThreadLocal<HashSet<string>> _visited = new(static () => new());

    private static readonly ThreadLocal<Queue<MacroNode>> _queue = new(static () => new());

    private static readonly ThreadLocal<HashSet<string>> _current = new(() => new());

#if DEBUG
    private static readonly ThreadLocal<Stopwatch> _stopwatch = new(() => new());
#endif

    internal static int CacheCount => pathCache.Count;

    internal static List<string>? GetPathFromCache(string start, string end, int gender) => pathCache.TryGetValue((start, end, gender), out List<string>? val) ? val : null;

    internal static void PrintCache()
    {
        Counter<int> counter = new();

        foreach (((string start, string end, int gender) key, List<string>? value) in pathCache)
        {
            ModEntry.ModMonitor.Log($"( {key.start} -> {key.end} ({key.gender})) == " + (value is not null ? string.Join("->", value) + $" [{value.Count}]" : "no path found" ), LogLevel.Info);

            if (value is null)
            {
                counter[0]++;
            }
            else
            {
                counter[value.Count]++;
            }
        }

        ModEntry.ModMonitor.Log($"In total: {pathCache.Count} routes cached", LogLevel.Info);
        foreach ((int key, int value) in counter)
        {
            ModEntry.ModMonitor.Log($"    {value} of length {key}", LogLevel.Info);
        }
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPatch(nameof(NPC.populateRoutesFromLocationToLocationList))]
    private static bool PrefixPopulateRoutes()
    {
        pathCache.Clear();

#if DEBUG
        _stopwatch.Value ??= new();
        _stopwatch.Value.Start();
#endif

        // pre-seed town a bit, since Town is basically a hub.
        _ = GetPathFor(Game1.getLocationFromName("Town"), null, ungendered, 3);

#if DEBUG
        _stopwatch.Value.Stop();
        ModEntry.ModMonitor.Log($"Total time so far: {_stopwatch.Value.ElapsedMilliseconds} ms, {pathCache.Count} total routes cached", LogLevel.Info);
#endif

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("getLocationRoute")]
    [HarmonyPriority(Priority.VeryLow)]
    private static bool PrefixGetLocationRoute(string startingLocation, string endingLocation, NPC __instance, ref List<string>? __result)
    {
        int gender = __instance.Gender switch
        {
            NPC.undefined => NPC.female,
            _ => __instance.Gender,
        };

        if (pathCache.TryGetValue((startingLocation, endingLocation, ungendered), out List<string>? cached)
            || pathCache.TryGetValue((startingLocation, endingLocation, gender), out cached))
        {
            ModEntry.ModMonitor.TraceOnlyLog($"Got macro schedule for {__instance.Name} from cache: {startingLocation} -> {endingLocation}");
            __result = cached;
            if (__result is null)
            {
                if (__instance.Gender == NPC.undefined)
                {
                    goto skip;
                }
                ModEntry.ModMonitor.Log($"{__instance.Name} requested path from {startingLocation} to {endingLocation} where no valid path was found.", LogLevel.Warn);
            }
            return false;
        }

        skip:
        if (__instance.Gender == NPC.undefined && pathCache.TryGetValue((startingLocation, endingLocation, NPC.male), out cached))
        {
            __result = cached;
            if (__result is null)
            {
                ModEntry.ModMonitor.Log($"{__instance.Name} requested path from {startingLocation} to {endingLocation} where no valid path was found.", LogLevel.Warn);
            }
            return false;
        }

        #region validation

        __result = null;
        if (GetActualLocation(endingLocation) is null)
        {
            ModEntry.ModMonitor.Log($"{__instance.Name} requested path to {endingLocation} which is blacklisted from pathing", LogLevel.Warn);
            return false;
        }

        GameLocation start = Game1.getLocationFromName(startingLocation);
        if (start is null)
        {
            ModEntry.ModMonitor.Log($"NPC {__instance.Name} requested path starting at {startingLocation}, which does not exist.", LogLevel.Error);
            return false;
        }
        int startGender = GetTightestGenderConstraint(__instance.Gender, GetGenderConstraint(startingLocation));
        if (startGender == invalid_gender)
        {
            ModEntry.ModMonitor.Log($"NPC {__instance.Name} requested path starting at {startingLocation}, which is not allowed due to their gender.", LogLevel.Error);
            return false;
        }

        GameLocation end = Game1.getLocationFromName(endingLocation);
        if (end is null)
        {
            ModEntry.ModMonitor.Log($"NPC {__instance.Name} requested path ending at {endingLocation}, which does not exist.", LogLevel.Error);
            return false;
        }
        int endGender = GetTightestGenderConstraint(__instance.Gender, GetGenderConstraint(endingLocation));
        if (endGender == invalid_gender)
        {
            ModEntry.ModMonitor.Log($"NPC {__instance.Name} requested path ending at {endingLocation}, which is not allowed due to their gender.", LogLevel.Error);
            return false;
        }

        #endregion

#if DEBUG
        _stopwatch.Value ??= new();
        _stopwatch.Value.Start();
#endif

        __result = GetPathFor(start, end, __instance.Gender);

#if DEBUG
        _stopwatch.Value.Stop();
        ModEntry.ModMonitor.Log($"Total time so far: {_stopwatch.Value.ElapsedMilliseconds} ms, {pathCache.Count} total routes cached", LogLevel.Info);
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

    private static List<string>? GetPathFor(GameLocation start, GameLocation? end, int gender, int limit = int.MaxValue)
    {
        try
        {
            _queue.Value ??= new();
            _queue.Value.Clear();
            _visited.Value ??= new();
            _visited.Value.Clear();

            int startGender = GetGenderConstraint(start.Name);
            _queue.Value.Enqueue(new(start.Name, null, startGender));

            while (_queue.Value.TryDequeue(out MacroNode? node))
            {
                _visited.Value.Add(node.name);

                if (Game1.getLocationFromName(node.name) is not GameLocation current)
                {
                    ModEntry.ModMonitor.LogOnce($"A warp references {node.name} which could not be found.", LogLevel.Warn);
                    continue;
                }

                // insert into cache
                if (node.depth > 0)
                {
                    List<string> route = Unravel(node);
                    pathCache.TryAdd((start.Name, node.name, node.genderConstraint), route);
                    int genderConstrainedToCurrentSearch = GetTightestGenderConstraint(gender, node.genderConstraint);

                    if (genderConstrainedToCurrentSearch != invalid_gender && end is not null)
                    {
                        // found destination, return it.
                        if (end.Name == node.name)
                        {
                            return route;
                        }

                        // if we have A->B and B->D, then we can string the path together already.
                        // avoiding trivial one-step stitching because this is more expensive to do.
                        if (pathCache.TryGetValue((node.name, end.Name, ungendered), out List<string>? prev)
                            && prev?.Count > 2 && CompletelyDistinct(route, prev))
                        {
                            ModEntry.ModMonitor.TraceOnlyLog($"Partial route found: {start.Name} -> {node.name} + {node.name} -> {end.Name}", LogLevel.Info);
                            List<string> routeStart = new(route.Count + prev.Count - 1);
                            routeStart.AddRange(route);
                            routeStart.RemoveAt(routeStart.Count - 1);
                            routeStart.AddRange(prev);

                            pathCache.TryAdd((start.Name, end.Name, node.genderConstraint), routeStart);
                            return routeStart;
                        }
                        else if (pathCache.TryGetValue((node.name, end.Name, genderConstrainedToCurrentSearch), out List<string>? genderedPrev)
                            && genderedPrev?.Count > 2 && CompletelyDistinct(route, genderedPrev))
                        {
                            ModEntry.ModMonitor.TraceOnlyLog($"Partial route found: {start.Name} -> {node.name} + {node.name} -> {end.Name}", LogLevel.Info);
                            List<string> routeStart = new(route.Count + genderedPrev.Count - 1);
                            routeStart.AddRange(route);
                            routeStart.RemoveAt(routeStart.Count - 1);
                            routeStart.AddRange(genderedPrev);

                            pathCache.TryAdd((start.Name, end.Name, genderConstrainedToCurrentSearch), routeStart);
                            return routeStart;
                        }
                    }
                }

                if (node.depth < limit)
                {
                    // queue next
                    foreach (string name in FindWarpsFrom(current, _visited.Value))
                    {
                        int genderConstraint = GetTightestGenderConstraint(node.genderConstraint, GetGenderConstraint(name));

                        // this path cannot possibly be valid due to genderlocking
                        if (genderConstraint == invalid_gender)
                        {
                            continue;
                        }

                        MacroNode next = new(name, node, genderConstraint);
                        _queue.Value.Enqueue(next);
                    }
                }
            }

            // queue exhausted.
            if (limit == int.MaxValue && end is not null)
            {
                // mark invalid.
                ModEntry.ModMonitor.Log($"Scheduler could not find route from {start.Name} to {end.Name} while honoring gender {gender}", LogLevel.Warn);
                pathCache.TryAdd((start.Name, end.Name, gender), null);
            }
            return null;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Error in rescheduler {ex}", LogLevel.Error);
            _visited.Value?.Clear();
            _queue.Value?.Clear();
            return null;
        }
    }

    private static bool CompletelyDistinct(List<string> route, List<string> prev)
    {
        for (int i = 0; i < prev.Count; i++)
        {
            string? second = prev[i];
            for (int j = route.Count - 2; j >= 0; j--)
            {
                string? first = route[j];
                if (first == second)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static List<string> Unravel(MacroNode node)
    {
        List<string> ret = new(node.depth + 1);
        for (int i = 0; i <= node.depth; i++)
        {
            ret.Add(string.Empty);
        }

        MacroNode? workingNode = node;
        do
        {
            ret[workingNode.depth] = workingNode.name;
            workingNode = workingNode.prev;
        }
        while (workingNode is not null);

        return ret;
    }

    /// <summary>
    /// Gets the locations leaving a specific place, keeping in mind the locations already visited.
    /// </summary>
    /// <param name="location">Location to look at.</param>
    /// <param name="visited">Previous visited locations.</param>
    /// <returns>IEnumerable of location names.</returns>
    /// <remarks>Stardew maps can have "duplicate" edges, must de-duplicate. This function takes ownership over the _current static hashset and should be the only function that mutates that.</remarks>
    private static IEnumerable<string> FindWarpsFrom(GameLocation? location, HashSet<string> visited)
    {
        if (location is null)
        {
            return Enumerable.Empty<string>();
        }

        _current.Value ??= new();
        _current.Value.Clear();

        if (location.warps?.Count is not null and not 0)
        {
            foreach (Warp? warp in location.warps)
            {
                string? name = GetActualLocation(warp.TargetName);
                if (name is not null && !visited.Contains(name))
                {
                    _current.Value.Add(name);
                }
            }
        }

        if (location.doors?.Count() is not 0 and not null)
        {
            foreach (string? door in location.doors.Values)
            {
                string? name = GetActualLocation(door);
                if (name is not null && !visited.Contains(name))
                {
                    _current.Value.Add(name);
                }
            }
        }

        return _current.Value;
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

    #region gender

    /// <summary>
    /// Given a map, get its gender restrictions.
    /// </summary>
    /// <param name="name">Name of map.</param>
    /// <returns>Gender to restrict to.</returns>
    private static int GetGenderConstraint(string name)
        => name switch
        {
            "BathHouse_MensLocker" => NPC.male,
            "BathHouse_WomensLocker" => NPC.female,
            _ => ungendered,
        };

    /// <summary>
    /// Given two gender constraints, return the tighter of the two.
    /// </summary>
    /// <param name="first">First gender constraint.</param>
    /// <param name="second">Second gender constraint.</param>
    /// <returns>Gender constraint, or null if not satisfiable.</returns>
    private static int GetTightestGenderConstraint(int first, int second)
    {
        if (first == ungendered || first == second)
        {
            return second;
        }
        if (second == ungendered)
        {
            return first;
        }
        return invalid_gender;
    }

    #endregion
}
