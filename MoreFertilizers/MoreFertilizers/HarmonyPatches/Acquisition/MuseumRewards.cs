﻿using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;
using StardewValley.Locations;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Holds patches to add things to the museum.
/// </summary>
[HarmonyPatch(typeof(LibraryMuseum))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class MuseumRewards
{
    /// <summary>
    /// Postfix on the museum to add the prismatic fertilizer as a reward item.
    /// </summary>
    /// <param name="__instance">museum instance.</param>
    /// <param name="who">farmer.</param>
    /// <param name="__result">List of items.</param>
    [HarmonyPatch(nameof(LibraryMuseum.getRewardsForPlayer))]
    private static void Postfix(LibraryMuseum __instance, Farmer who, List<Item> __result)
    {
        try
        {
            if (__instance.museumPieces.Values.Contains(74) && ModEntry.PrismaticFertilizerID != -1)
            { // prismatic shard = 74
                __instance.AddRewardIfUncollected(
                    farmer: who,
                    rewards: __result,
                    reward_item: new SObject(ModEntry.PrismaticFertilizerID, 5));
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding museum rewards", ex);
        }
    }
}
