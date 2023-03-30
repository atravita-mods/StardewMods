using HarmonyLib;

namespace CameraPan.HarmonyPatches;

/// <summary>
/// Patches on events.
/// </summary>
[HarmonyPatch]
internal static class EventPatches
{
    [HarmonyPatch(typeof(Game1), nameof(Game1.eventFinished))]
    private static void PostfixEventEnd()
        => ModEntry.snapOnNextTick.Value = true;
}
