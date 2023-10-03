using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using GrowableGiantCrops.Framework;
using GrowableGiantCrops.Framework.InventoryModels;

using HarmonyLib;

using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.HarmonyPatches.Niceties;

/// <summary>
/// Patches to handle updating trees seasonally.
/// </summary>
[HarmonyPatch(typeof(Tree))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class SeasonalTreeUpdates
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Tree.dayUpdate))]
    private static void PrefixDayUpdate(Tree __instance, GameLocation environment)
    {
        try
        {
            if (!ModEntry.Config.PalmTreeBehavior.HasFlagFast(PalmTreeBehavior.Stump) || __instance.health.Value <= -100f || environment is Desert or MineShaft or IslandLocation
                || __instance.modData?.ContainsKey(InventoryTree.ModDataKey) != true)
            {
                return;
            }

            if (__instance.treeType.Value is Tree.palmTree or Tree.palmTree2)
            {
                if (Game1.GetSeasonForLocation(__instance.Location) == Season.Winter)
                {
                    __instance.stump.Value = true;
                }
                else if (Game1.dayOfMonth <= 1 && Game1.IsSpring)
                {
                    __instance.stump.Value = false;
                    __instance.health.Value = 10f;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("updating tree texture", ex);
        }
    }
}
