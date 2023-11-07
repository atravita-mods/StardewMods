using HarmonyLib;

using static StardewValley.Event;

namespace SleepInWedding.HarmonyPatches;

/// <summary>
/// Removes the multiplayer sync at the end of the wedding.
/// </summary>
[HarmonyPatch(typeof(DefaultCommands))]
internal static class RemoveEndWeddingCheck
{
    [HarmonyPatch(nameof(DefaultCommands.WaitForOtherPlayers))]
    private static bool Prefix(Event @event)
    {
        if (@event.isWedding)
        {
            @event.CurrentCommand++;
            return false;
        }
        return true;
    }
}
