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
    {
        ModEntry.Reset();
        ModEntry.SnapOnNextTick = true;
    }

    [HarmonyPatch(typeof(Event), nameof(Event.endBehaviors))]
    private static void PostfixEndBehaviors()
    {
        ModEntry.Reset();
        ModEntry.SnapOnNextTick = true;
    }
}
