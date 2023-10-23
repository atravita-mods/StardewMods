using System.Reflection;
using System.Reflection.Emit;

using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;

using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;

namespace ForgeMenuChoice.HarmonyPatches;

/// <summary>
/// Patch that overrides which enchantment is applied when forging.
/// This has to be a transpiler - I need only this call to GetEnchantmentFromItem to be different.
/// </summary>
[HarmonyPatch(typeof(Tool))]
internal static class GetEnchantmentPatch
{
    /// <summary>
    /// Applies a patch against EnchantableScythes.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    internal static void ApplyPatch(Harmony harmony)
    {
        try
        {
            Type scythePatcher = AccessTools.TypeByName("ScytheFixes.Patcher")
                ?? ReflectionThrowHelper.ThrowMethodNotFoundException<Type>("EnchantableScythes");
            harmony.Patch(
                original: scythePatcher.GetCachedMethod("Forge_Post", ReflectionCache.FlagTypes.StaticFlags),
                transpiler: new HarmonyMethod(typeof(GetEnchantmentPatch), nameof(Transpiler)));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("transpiling EnchantableScythes", ex);
        }
    }

    /// <summary>
    /// Function that substitutes in an enchantment.
    /// </summary>
    /// <param name="base_item">Tool.</param>
    /// <param name="item">Thing to enchant with.</param>
    /// <returns>Enchantment to substitute in.</returns>
    private static BaseEnchantment SubstituteEnchantment(Item base_item, Item item)
    {
        try
        {
            if (Utility.IsNormalObjectAtParentSheetIndex(item, 74) && ForgeMenuPatches.CurrentSelection is not null)
            {
                BaseEnchantment output = ForgeMenuPatches.CurrentSelection;
                ForgeMenuPatches.TrashMenu();
                return output;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("forcing selection of enchantment", ex);
        }
        return BaseEnchantment.GetEnchantmentFromItem(base_item, item);
    }

    [HarmonyPatch(nameof(Tool.Forge))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindFirst(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldarg_0),
                new(SpecialCodeInstructionCases.LdArg),
                new(OpCodes.Call, typeof(BaseEnchantment).GetCachedMethod(nameof(BaseEnchantment.GetEnchantmentFromItem), ReflectionCache.FlagTypes.StaticFlags)),
                new(SpecialCodeInstructionCases.StLoc),
                new(SpecialCodeInstructionCases.LdLoc),
            })
            .Advance(2)
            .ReplaceOperand(typeof(GetEnchantmentPatch).GetCachedMethod(nameof(GetEnchantmentPatch.SubstituteEnchantment), ReflectionCache.FlagTypes.StaticFlags));
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}