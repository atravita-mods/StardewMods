using HarmonyLib;
using SpecialOrdersExtended.Managers;

namespace SpecialOrdersExtended.HarmonyPatches;

/// <summary>
/// Holds patches against Special Orders.
/// </summary>
[HarmonyPatch(typeof(SpecialOrder))]
internal static class SpecialOrderPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(SpecialOrder.OnFail))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention.")]
    private static void PostfixOnFail(SpecialOrder __instance) => DialogueManager.ClearOnFail(__instance.questKey.Value);
}