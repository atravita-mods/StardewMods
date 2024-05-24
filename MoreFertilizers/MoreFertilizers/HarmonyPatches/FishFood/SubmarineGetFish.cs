﻿using AtraCore;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using MoreFertilizers.Framework;

using StardewValley.Locations;

namespace MoreFertilizers.HarmonyPatches.FishFood;

/// <summary>
/// Classes that holds patches against Submarine's GetFish.
/// </summary>
[HarmonyPatch(typeof(Submarine))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class SubmarineGetFish
{
    [HarmonyPatch(nameof(Submarine.getFish))]
    private static bool Prefix(GameLocation __instance, ref SObject __result)
    {
        try
        {
            if (__instance?.modData?.GetInt(CanPlaceHandler.FishFood) > 0)
            {
                int[] fishies = new[] { 800, 799, 798, 154, 155, 149 };
                __result = new SObject(fishies[Singletons.Random.Next(fishies.Length)], 1);
                return false;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("replacing submarine getfish", ex);
        }
        return true;
    }
}