using System.Reflection;
using System.Reflection.Emit;

using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;

using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;

using StardewValley.Enchantments;
using StardewValley.Tools;

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
            if (item.QualifiedItemId == "(O)74" && ForgeMenuPatches.CurrentSelection is not null)
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

    private static Item SubstituteInnateEnchantment(Item weapon, Random r, bool force, List<BaseEnchantment>? enchantsToReRoll = null)
    {
        if (weapon is not MeleeWeapon w || ForgeMenuPatches.CurrentSelection is not { } selection)
        {
            return MeleeWeapon.attemptAddRandomInnateEnchantment(weapon, r, force, enchantsToReRoll);
        }
        w.enchantments.Add(ForgeMenuPatches.CurrentSelection);
        ForgeMenuPatches.TrashMenu();

        return weapon;
    }

    [HarmonyPatch(nameof(Tool.Forge))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindFirst(
            [
                new(OpCodes.Ldarg_0),
                new(SpecialCodeInstructionCases.LdArg),
                new(OpCodes.Call, typeof(BaseEnchantment).GetCachedMethod(nameof(BaseEnchantment.GetEnchantmentFromItem), ReflectionCache.FlagTypes.StaticFlags)),
                new(SpecialCodeInstructionCases.StLoc),
                new(SpecialCodeInstructionCases.LdLoc),
            ])
            .Advance(2)
            .ReplaceOperand(typeof(GetEnchantmentPatch).GetCachedMethod(nameof(SubstituteEnchantment), ReflectionCache.FlagTypes.StaticFlags))
            .FindNext(
                [
                    new(OpCodes.Call, typeof(MeleeWeapon).GetCachedMethod(nameof(MeleeWeapon.attemptAddRandomInnateEnchantment), ReflectionCache.FlagTypes.StaticFlags))
                ])
            .ReplaceOperand(typeof(GetEnchantmentPatch).GetCachedMethod(nameof(SubstituteInnateEnchantment), ReflectionCache.FlagTypes.StaticFlags));
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}