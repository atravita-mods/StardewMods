using AtraShared.Utils.Extensions;
using HarmonyLib;
using MiniAtraShared.Extensions;
using MoreFertilizers.Framework;
using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.FruitTreePatches;

[HarmonyPatch(typeof(FruitTree))]
internal static class FruitTreePatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(FruitTree.dayUpdate))]
    private static void PostfixDayUpdate(FruitTree __instance)
    {
        if (!__instance.stump.Value && __instance.fruit.Count <= 0 && __instance.growthStage.Value == FruitTree.treeStage
            && !__instance.IgnoresSeasonsHere()
            && __instance.modData?.GetBool(CanPlaceHandler.EverlastingFruitTreeFertilizer) == true)
        {
            try
            {
                __instance.GreenHouseTree = true;
                __instance.TryAddFruit();
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError("adding fruit to fruit tree", ex);
            }
            finally
            {
                __instance.GreenHouseTree = false;
            }
        }
    }
}
