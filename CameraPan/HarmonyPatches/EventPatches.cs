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
        yield return AccessTools.Method(typeof(Event), nameof(Event.endBehaviors));
    }

    [HarmonyPostfix]
    private static void PostfixEventEnd()
    {
        DelayedAction.functionAfterDelay(
            () =>
            {
                Microsoft.Xna.Framework.Point pos = Game1.player.getStandingXY();
                ModEntry.Reset();
                ModEntry.SnapOnNextTick = true;
            },
            25);
    }
}
