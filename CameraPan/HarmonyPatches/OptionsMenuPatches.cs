using HarmonyLib;

using StardewValley.Menus;

namespace CameraPan.HarmonyPatches;

/// <summary>
/// Patches on the options menu.
/// </summary>
[HarmonyPatch(typeof(DayTimeMoneyBox))]
internal static class OptionsMenuPatches
{
    /// <summary>
    /// Updates the position of our button in relation to the day time money box if needed.
    /// </summary>
    [HarmonyPatch("updatePosition")]
    private static void Postfix() => ModEntry.CreateOrModifyButton();
}
