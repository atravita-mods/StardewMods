﻿using AtraBase.Toolkit.Extensions;

using AtraShared.Caching;
using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using MoreFertilizers.Framework;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

#warning - check for multiplayer compat here.

/// <summary>
/// Applies patches against the traveling merchant.
/// </summary>
[HarmonyPatch(typeof(Utility))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class TravelingMerchantPatcher
{
    private static readonly TickCache<bool> HasPlayerUnlockedBountiful = new(() => Game1.MasterPlayer.mailReceived.Contains(AssetEditor.BOUNTIFUL_BUSH_UNLOCK));

    [UsedImplicitly]
    [HarmonyPatch("generateLocalTravelingMerchantStock")]
    private static void Postfix(Dictionary<ISalable, int[]> __result, int seed)
    {
        try
        {
            Random random = new(seed);
            random.PreWarm();
            if (ModEntry.SecretJojaFertilizerID != -1 && Game1.player.DailyLuck > 0.5 && random.OfChance(0.05))
            {
                __result.Add(new SObject(ModEntry.SecretJojaFertilizerID, 1), new[] { (int)(2500 * Game1.player.difficultyModifier), 1 });
            }

            if (ModEntry.BountifulBushID != -1 && Game1.currentSeason is "spring" or "fall" && HasPlayerUnlockedBountiful.GetValue())
            {
                __result.Add(new SObject(ModEntry.BountifulBushID, 1), new[] { 200, random.Next(1, 3) });
            }
            else if (ModEntry.WisdomFertilizerID != -1 && Game1.currentSeason is "summer" or "winter")
            {
                __result.Add(new SObject(ModEntry.WisdomFertilizerID, 1), new[] { 100, random.Next(1, 3) });
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding to traveling cart", ex);
        }
    }
}
