//#define TRACELOG

using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Tools;

namespace ExperimentalLagReduction.HarmonyPatches;

[HarmonyPatch]
internal static class FishingDistanceCache
{
    private static readonly Dictionary<string, Dictionary<(int x, int y, bool land), int>> _cache = [];

    [HarmonyPostfix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(FishingRod), nameof(FishingRod.distanceToLand))]
    private static void PostfixDistanceToLand(int tileX, int tileY, GameLocation location, bool landMustBeAdjacentToWalkableTile, bool __runOriginal, int __result)
    {
        if (!__runOriginal)
        {
            return;
        }
        if (!_cache.TryGetValue(location.NameOrUniqueName, out Dictionary<(int x, int y, bool land), int>? d))
        {
            _cache[location.NameOrUniqueName] = d = [];
        }

        d[(tileX, tileY, landMustBeAdjacentToWalkableTile)] = __result;
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(FishingRod), nameof(FishingRod.distanceToLand))]
    private static bool PrefixDistanceToLand(int tileX, int tileY, GameLocation location, bool landMustBeAdjacentToWalkableTile, ref int __result)
    {
        if (_cache.TryGetValue(location.NameOrUniqueName, out Dictionary<(int x, int y, bool land), int>? d)
            && d.TryGetValue((tileX, tileY, landMustBeAdjacentToWalkableTile), out int distance))
        {
            ModEntry.ModMonitor.TraceOnlyLog($"Cache hit {location.NameOrUniqueName} - tile {tileX}, {tileY}");
            __result = distance;
            return false;
        }
        ModEntry.ModMonitor.TraceOnlyLog($"Cache miss {location.NameOrUniqueName} - tile {tileX}, {tileY}");
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.MakeMapModifications))]
    private static void PostfixMapModifcations(GameLocation __instance, bool force)
    {
        if (force)
        {
            _cache.Remove(__instance.NameOrUniqueName);
            ModEntry.ModMonitor.TraceOnlyLog($"Removing {__instance.NameOrUniqueName} from cache");
        }
    }
}
