using AtraBase.Toolkit.Extensions;

using AtraCore;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using MoreFertilizers.Framework;

using StardewValley.Objects;

namespace MoreFertilizers.HarmonyPatches.OrganicFertilizer;

/// <summary>
/// Handles organic seeds for indoor pots.
/// </summary>
[HarmonyPatch(typeof(IndoorPot))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class IndoorPotPlacement
{
    [HarmonyPatch(nameof(IndoorPot.performObjectDropInAction))]
    private static void Postfix(IndoorPot __instance, Item? dropInItem, bool probe)
    {
        if (probe)
        {
            return;
        }
        if (dropInItem?.modData?.GetBool(CanPlaceHandler.Organic) == true
            && __instance.hoeDirt?.Value?.fertilizer is { } fertilizer
            && fertilizer.Value is null
            && Random.Shared.OfChance(0.5))
        {
            __instance.hoeDirt.Value.fertilizer.Value = ModEntry.OrganicFertilizerID;
        }
    }
}