using HarmonyLib;

namespace CameraPan.HarmonyPatches;

/// <summary>
/// Patches against GameLocation.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
internal static class GameLocationPatches
{
    [HarmonyPatch(nameof(GameLocation.startEvent))]
    private static void Prefix()
    {
        ModEntry.Reset();
        ModEntry.SnapOnNextTick = true;
    }
}
