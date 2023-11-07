using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

namespace GrowableGiantCrops.HarmonyPatches.GrassPatches;

/// <summary>
/// Adds patches against Item.
/// </summary>
[HarmonyPatch(typeof(Item))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class ItemPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Item.canStackWith))]
    private static bool PrefixCanStackWith(Item __instance, ISalable other, ref bool __result)
    {
        if (other is null || __instance.QualifiedItemId != SObjectPatches.GrassStarterQualId)
        {
            return true;
        }

        try
        {
            if (other is SObject otherItem)
            {
                string? myData = null;
                string? otherData = null;
                _ = __instance.modData?.TryGetValue(SObjectPatches.ModDataKey, out myData);
                _ = otherItem.modData?.TryGetValue(SObjectPatches.ModDataKey, out otherData);
                if (myData != otherData
                    || (myData is not null && ModEntry.Config.PreserveModData && !__instance.modData.ModDataMatches(otherItem.modData)))
                {
                    __result = false;
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("overwriting stacking behavior", ex);
        }

        return true;
    }
}
