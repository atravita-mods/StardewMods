using HarmonyLib;

namespace CritterRings.HarmonyPatches.BunnyRing;

/// <summary>
/// Patches against movement speed.
/// </summary>
[HarmonyPatch(typeof(Farmer))]
internal static class MovementSpeedPatch
{
    [HarmonyPatch(nameof(Farmer.getMovementSpeed))]
    private static void Prefix(out bool __state)
    {
        __state = false;
        if (Game1.CurrentEvent?.isFestival == true && Game1.eventUp && Game1.player.hasBuff(ModEntry.BunnyBuffId))
        {
            Game1.eventUp = false;
            __state = true;
        }
    }

    [HarmonyPatch(nameof(Farmer.getMovementSpeed))]
    private static void Finalizer(bool __state)
    {
        if (__state)
        {
            Game1.eventUp = true;
        }
    }
}
