using HarmonyLib;

using Microsoft.Xna.Framework;

using MiniAtraShared.Extensions;

using StardewValley.Buildings;

namespace AtraCore.HarmonyPatches.FishPondPatches;

/// <summary>
/// A patch on fish colors to override with the tailoring color for all fishes, if that was enabled.
/// </summary>
[HarmonyPatch(typeof(FishPond))]
internal static class FishColorPatches
{
    [HarmonyPatch("doFishSpecificWaterColoring")]
    private static void Postfix(FishPond __instance)
    {
        try
        {
            if (__instance.overrideWaterColor.Value != Color.White || !ModEntry.Config.AutoColorFishPonds)
            {
                return;
            }

            __instance.overrideWaterColor.Value = ItemContextTagManager.GetColorFromTags(ItemRegistry.Create(ItemRegistry.type_object + __instance.fishType.Value)) ?? Color.White;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("coloring ponds", ex);
        }
    }
}
