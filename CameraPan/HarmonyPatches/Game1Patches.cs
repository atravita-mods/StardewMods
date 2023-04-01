using HarmonyLib;

namespace CameraPan.HarmonyPatches;

/// <summary>
/// Holds patches against Game1.
/// </summary>
[HarmonyPatch(typeof(Game1))]
internal static class Game1Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Game1.afterFadeReturnViewportToPlayer))]
    private static void PostfixAfterFade() => ModEntry.SnapOnNextTick = true;
}
