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

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(nameof(SpecialOrder.UpdateAvailableSpecialOrders))]
    private static bool PrefixUpdate() => SpecialOrder.IsSpecialOrdersBoardUnlocked();

    [HarmonyPrefix]
    [HarmonyPriority(Priority.HigherThanNormal)]
    [HarmonyPatch(nameof(SpecialOrder.SetDuration))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
    private static bool PrefixSetDuration(SpecialOrder __instance)
    {
        try
        {
            Dictionary<string, int> overrides = AssetManager.GetDurationOverride();
            if (overrides.TryGetValue(__instance.questKey.Value, out int duration))
            {
                WorldDate? date = new(Game1.year, Game1.currentSeason, Game1.dayOfMonth);
                __instance.dueDate.Value = date.TotalDays + (duration == -1 ? 99 : duration);
                return true;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod failed while trying to override special order duration!\n\n{ex}");
        }
        return false;
    }
}