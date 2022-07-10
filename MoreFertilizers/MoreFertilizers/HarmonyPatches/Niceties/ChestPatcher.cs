﻿using HarmonyLib;
using StardewValley.Objects;

namespace MoreFertilizers.HarmonyPatches.Niceties;

/// <summary>
/// Holds patches against Chests.
/// </summary>
[HarmonyPatch(typeof(Chest))]
internal static class ChestPatcher
{
    [HarmonyPatch(nameof(Chest.dumpContents))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention")]
    private static void Postfix(Chest __instance, GameLocation location)
    {
        if (__instance.giftbox.Value && ModEntry.LuckyFertilizerID != -1)
        {
            Game1.createMultipleObjectDebris(
                index: ModEntry.LuckyFertilizerID,
                xTile: (int)__instance.TileLocation.X,
                yTile: (int)__instance.TileLocation.Y,
                number: 5,
                location: location);
        }
    }
}