using System.Runtime.CompilerServices;

using AtraBase.Toolkit;
using AtraBase.Toolkit.Extensions;

using HarmonyLib;

using StardewValley.Monsters;

namespace CritterRings.HarmonyPatches.OwlRing;

/// <summary>
/// Patches so monsters have to be closer to you to see them.
/// </summary>
[HarmonyPatch(typeof(NPC))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
internal static class BaseSightPatch
{
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPatch(nameof(NPC.withinPlayerThreshold), new[] { typeof(int) } )]
    private static void Prefix(NPC __instance, ref int threshold)
    {
        if (__instance is not Monster)
        {
            return;
        }

        if (ModEntry.OwlRing > 0 && Game1.player.isWearingRing(ModEntry.OwlRing))
        {
            threshold /= 2;
            threshold += threshold / 2;
        }
    }
}
