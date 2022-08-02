using AtraCore.Framework.ReflectionManager;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace GiantCropFertilizer.HarmonyPatches;
internal static class FixSaveThing
{
    internal static void ApplyPatches(Harmony harmony)
    {
        harmony.Patch(
            original: typeof(GameLocation).GetCachedMethod(nameof(GameLocation.TransferDataFromSavedLocation), ReflectionCache.FlagTypes.InstanceFlags),
            postfix: new HarmonyMethod(typeof(FixSaveThing), nameof(Postfix)));
    }

    private static void Postfix(GameLocation __instance, GameLocation l)
    {
        // game handles these two.
        if (__instance is IslandWest || __instance.Name.Equals("Farm"))
        {
            return;
        }

        // We need to avoid accidentally adding duplicates.
        // Keep track of occupied tiles here.
        HashSet<Vector2> prev = new(l.resourceClumps.Count);

        foreach (var clump in __instance.resourceClumps)
        {
            prev.Add(clump.tile.Value);
        }

        // restore previous giant crops.
        foreach (var clump in l.resourceClumps)
        {
            if (clump is GiantCrop crop && prev.Add(crop.tile.Value))
            {
                __instance.resourceClumps.Add(crop);
            }
        }
    }
}
