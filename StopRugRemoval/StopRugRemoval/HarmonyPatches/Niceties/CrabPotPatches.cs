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
    /// Surpresses drawing crabpots when an event is up.
    /// </summary>
    /// <returns>True to continue (and draw), false to surpress drawing.</returns>
    [HarmonyPatch(nameof(CrabPot.draw))]
    private static bool PrefixDraw()
        => !ModEntry.Config.HideCrabPots || !Game1.eventUp;
}