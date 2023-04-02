using System.Runtime.CompilerServices;

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
    private static void PostfixAfterFade() => ResetAndSnap();

    [HarmonyPrefix]
    [HarmonyPatch(nameof(Game1.globalFadeToClear))]
    private static void PrefixFadeToClear() => ResetAndSnap();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ResetAndSnap()
    {
        ModEntry.SnapOnNextTick = true;
        ModEntry.Reset();
    }
}
