using AtraBase.Toolkit.Extensions;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;
using StardewValley.Locations;
using StardewValley.Tools;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Postfixes Pan.getPanItems to add these fertilizers.
/// </summary>
[HarmonyPatch(typeof(Pan))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class PanGetPanItemsPostfix
{
    [HarmonyPriority(Priority.VeryLow)] // behind other panning mods
    [HarmonyPatch(nameof(Pan.getPanItems))]
    private static void Postfix(GameLocation location, ref List<Item> __result)
    {
        try
        {
            if (!Random.Shared.OfChance(0.04))
            {
                return;
            }

            if (location is Town && ModEntry.LuckyFertilizerID != -1)
            {
                __result.Add(new SObject(ModEntry.LuckyFertilizerID, 5));
            }
            else if (location is Farm && ModEntry.WisdomFertilizerID != -1 && Game1.player.FarmingLevel > 4)
            {
                __result.Add(new SObject(ModEntry.WisdomFertilizerID, 5));
            }
            else if (location is IslandLocation && ModEntry.OrganicFertilizerID != -1)
            {
                __result.Add(new SObject(ModEntry.OrganicFertilizerID, 5));
            }
            else if (location is Sewer)
            {
                if (ModEntry.SecretJojaFertilizerID != -1 && Random.Shared.OfChance(0.1)
                    && Utility.hasFinishedJojaRoute())
                {
                    __result.Add(new SObject(ModEntry.SecretJojaFertilizerID, 2));
                }
                else if (ModEntry.DeluxeJojaFertilizerID != -1)
                {
                    __result.Add(new SObject(ModEntry.DeluxeJojaFertilizerID, 5));
                }
            }
            else if (location is BugLand && ModEntry.BountifulFertilizerID != -1)
            {
                __result.Add(new SObject(ModEntry.BountifulFertilizerID, 5));
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding to pan's items", ex);
        }
    }
}