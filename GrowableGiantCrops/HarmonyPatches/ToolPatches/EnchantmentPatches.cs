using GrowableGiantCrops.Framework;

using HarmonyLib;

namespace GrowableGiantCrops.HarmonyPatches.ToolPatches;

/// <summary>
/// Patches to disable enchantments on shovels for now.
/// </summary>
[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Named for harmony.")]
internal static class EnchantmentPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SwiftToolEnchantment), nameof(SwiftToolEnchantment.CanApplyTo))]
    private static void SwiftPostfix(ref bool __result, Item item)
    {
        if (__result && item is ShovelTool)
        {
            __result = false;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(EfficientToolEnchantment), nameof(EfficientToolEnchantment.CanApplyTo))]
    private static void EfficientPostfix(ref bool __result, Item item)
    {
        if (__result && item is ShovelTool)
        {
            __result = false;
        }
    }
}
