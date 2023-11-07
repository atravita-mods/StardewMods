
using HarmonyLib;

using StardewValley.SpecialOrders;

namespace EastScarp.HarmonyPatches;

/// <summary>
/// Holds patches against Special Orders.
/// </summary>
[HarmonyPatch(typeof(SpecialOrder))]
internal static class SpecialOrderPatches
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.HigherThanNormal)]
    [HarmonyPatch(nameof(SpecialOrder.SetDuration))]
    private static bool PrefixSetDuration(SpecialOrder __instance)
    {
        try
        {
            Dictionary<string, string> overrides = AssetManager.GetDurationOverride();
            if (overrides.TryGetValue(__instance.questKey.Value, out string? val))
            {
                if (int.TryParse(val, out int duration))
                {
                    WorldDate? date = new(Game1.year, Game1.currentSeason, Game1.dayOfMonth);
                    __instance.dueDate.Value = date.TotalDays + (duration == -1 ? 99 : duration);
                    return false;
                }
                else
                {
                    ModEntry.ModMonitor.LogOnce($"Special order {__instance.questKey.Value} specified {val} as override which was not parsable as integer.", LogLevel.Error);
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("overriding special order duration", ex);
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.HigherThanNormal)]
    [HarmonyPatch(nameof(SpecialOrder.IsTimedQuest))]
    private static void HandleUntimed(SpecialOrder __instance, ref bool __result)
    {
        if (__result && AssetManager.Untimed.Value.Contains(__instance.questKey.Value))
        {
            __result = false;
        }
    }
}