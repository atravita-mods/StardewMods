using CritterRings;

using HarmonyLib;

using StardewValley.Tools;

[HarmonyPatch]
internal static class JumpPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.leftClick))]
    private static bool PrefixSwordSwing(Farmer who)
    {
        return ModEntry.CurrentJumper?.IsValid(out Farmer? farmer) != true || !ReferenceEquals(who, farmer);
    }

    [HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.doSwipe))]
    private static bool PrefixSwordSwipe(Farmer f)
    {
        return ModEntry.CurrentJumper?.IsValid(out Farmer? farmer) != true || !ReferenceEquals(f, farmer);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Game1), nameof(Game1.pressUseToolButton))]
    private static bool PrefixUseTool()
    {
        return ModEntry.CurrentJumper?.IsValid(out Farmer? farmer) != true;
    }
}