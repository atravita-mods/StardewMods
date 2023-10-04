namespace AtraCore.HarmonyPatches.ContextTagPatches;

using AtraBase.Toolkit.Extensions;

using AtraShared.ConstantsAndEnums;
using AtraShared.Wrappers;

using HarmonyLib;

/// <summary>
/// Patches against the Utility class.
/// </summary>
[HarmonyPatch(typeof(Utility))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class UtilityPatcher
{
    [HarmonyPatch(nameof(Utility.isObjectOffLimitsForSale))]
    private static void Postfix(string id, ref bool __result)
    {
        if (!__result)
        {
            __result = Game1Wrappers.ObjectData.GetValueOrGetDefault(id)?.CustomFields?.ContainsKey("OffLimitsForSale") == true;
        }
    }
}
