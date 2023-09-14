using HarmonyLib;

namespace CritterRings.HarmonyPatches.BunnyRing;

/// <summary>
/// Changes the base chance of bunnies spawning if the bunny ring is equipped.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
internal static class BaseChanceModifier
{
    [HarmonyPatch(nameof(GameLocation.addBunnies))]
    private static void Prefix(ref double chance)
    {
        if (Game1.player.isWearingRing(ModEntry.BunnyRing))
        {
            chance = 1.1;
        }
    }
}
