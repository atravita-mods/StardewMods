using HarmonyLib;

using StardewValley.Menus;

namespace CameraPan.HarmonyPatches;

/// <summary>
/// Patches the shop to put in the camera.
/// </summary>
[HarmonyPatch(typeof(Utility))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class ShopPatcher
{
    [HarmonyPatch(nameof(Utility.getCarpenterStock))]
    private static void Postfix(Dictionary<ISalable, int[]> __result)
    {
        __result.TryAdd(new SObject(ModEntry.CAMERA_ID, 1), new[] { 2_000, ShopMenu.infiniteStock });
    }
}
