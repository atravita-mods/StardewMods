using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using MoreFertilizers.Framework;

namespace MoreFertilizers.HarmonyPatches.FishFood;

/// <summary>
/// Resets the water color to the fed fish color if needed.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class GameLocationResetWater
{
    [UsedImplicitly]
    [HarmonyPatch("resetForPlayerEntry")]
    private static void Postfix(GameLocation __instance)
    {
        try
        {
            if (__instance?.modData?.GetInt(CanPlaceHandler.FishFood) is > 0)
            {
                __instance.waterColor.Value = ModEntry.Config.WaterOverlayColor;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"adjusting water color at location {__instance.NameOrUniqueName}", ex);
        }
    }
}