namespace GrowableGiantCrops.HarmonyPatches.ToolPatches;

using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraShared.ConstantsAndEnums;

using GrowableGiantCrops.Framework;

using HarmonyLib;

using StardewValley.Enchantments;

/// <summary>
/// Patches for enchantments for the shovel.
/// </summary>
[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class EnchantmentPatches
{
    [HarmonyPostfix]
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPatch(typeof(HoeEnchantment), nameof(HoeEnchantment.CanApplyTo))]
    private static void OverrideHoeCanApplyTo(Item item, ref bool __result)
    {
        if (!__result && ModEntry.Config.AllowHoeEnchantments && item is ShovelTool)
        {
            __result = true;
        }
    }
}
