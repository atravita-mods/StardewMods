namespace CritterRings.HarmonyPatches.FrogRing;

using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using StardewValley.Tools;

/// <summary>
/// Patches to make sure the player doesn't move in certain times.
/// </summary>
[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class JumpPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.leftClick))]
    private static bool PrefixSwordSwing(Farmer who)
        => ModEntry.CurrentJumper?.IsValid(out Farmer? farmer) != true || !ReferenceEquals(who, farmer);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Game1), nameof(Game1.pressUseToolButton))]
    private static bool PrefixUseTool()
        => ModEntry.CurrentJumper?.IsValid(out Farmer? _) != true;

    [HarmonyPostfix]
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPatch(typeof(Farmer), nameof(Farmer.getDrawLayer))]
    private static void PostfixGetDrawLayer(Farmer __instance, ref float __result)
    {
        const float factor = 0.0035f;
        switch (MathF.Sign(__instance.yJumpVelocity))
        {
            // player rising.
            case 1:

                // and moving forward
                if (__instance.Position.Y - __instance.lastPosition.Y > 0)
                {
                    __result -= factor;
                    return;
                }

                __result += factor;
                return;

            // player falling
            case -1:

                // and moving backwards
                if (__instance.Position.Y - __instance.lastPosition.Y < 0)
                {
                    __result -= factor;
                    return;
                }

                __result += factor;
                return;
        }
    }
}