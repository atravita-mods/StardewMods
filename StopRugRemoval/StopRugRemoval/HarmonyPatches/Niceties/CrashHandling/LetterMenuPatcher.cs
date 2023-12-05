using HarmonyLib;

using StardewValley.Menus;

namespace StopRugRemoval.HarmonyPatches.Niceties.CrashHandling;

/// <summary>
/// Patches against letter menu.
/// </summary>
[HarmonyPatch(typeof(LetterViewerMenu))]
internal static class LetterMenuPatcher
{
    private static bool Prepare() => ModEntry.ModMonitor.IsVerbose;

    [HarmonyFinalizer]
    [HarmonyPatch(nameof(LetterViewerMenu.HandleItemCommand))]
    private static void LogError(LetterViewerMenu __instance, Exception __exception)
    {
        if (__exception is not null)
        {
            ModEntry.ModMonitor.Log($"{__instance.mailTitle} - {string.Join('\n', __instance.mailMessage)} has error: {__exception}", LogLevel.Info);
        }
    }
}
