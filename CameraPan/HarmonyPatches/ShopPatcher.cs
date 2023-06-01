using HarmonyLib;

using StardewValley.Menus;

namespace CameraPan.HarmonyPatches;

/// <summary>
/// Patches the shop to put in the camera.
/// </summary>
[HarmonyPatch(typeof(Utility))]
internal static class ShopPatcher
{
    [HarmonyPatch(nameof(Utility.getCarpenterStock))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Named for Harmony.")]
    private static void Postfix(Dictionary<ISalable, int[]> __result)
    {
        __result.Add(new SObject(ModEntry.CAMERA_ID, 1), new[] { 2_000, ShopMenu.infiniteStock });
    }
}
