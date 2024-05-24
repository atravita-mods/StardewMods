using HarmonyLib;
using StardewValley.Objects;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Holds patches against crab pots.
/// </summary>
[HarmonyPatch(typeof(CrabPot))]
internal static class CrabPotPatches
{
    /// <summary>
    /// Suppresses drawing crabpots when an event is up.
    /// </summary>
    /// <returns>True to continue (and draw), false to suppress drawing.</returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(CrabPot.draw))]
    private static bool PrefixDraw()
        => !ModEntry.Config.HideCrabPots || !(Game1.eventUp || Game1.isFestival()) || !ModEntry.Config.Enabled;
}