using System.Reflection;

using HarmonyLib;

namespace CameraPan.HarmonyPatches;

/// <summary>
/// Patches on events.
/// </summary>
[HarmonyPatch]
internal static class EventPatches
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Game1), nameof(Game1.eventFinished));
        yield return AccessTools.Method(typeof(Event), nameof(Event.endBehaviors), new[] { typeof(string[]), typeof(GameLocation) });
    }

    [HarmonyPostfix]
    private static void PostfixEventEnd()
    {
        DelayedAction.functionAfterDelay(
            () =>
            {
                ModEntry.Reset();
                ModEntry.SnapOnNextTick = true;
            },
            25);
    }
}
