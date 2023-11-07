using System.Reflection;
using System.Reflection.Emit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using StardewValley.Menus;

namespace PrismaticClothing.HarmonyPatches;

/// <summary>
/// Holds a transpiler against TailoringMenu.CraftItem.
/// </summary>
[HarmonyPatch(typeof(TailoringMenu))]
internal static class TranspileCraftItem
{
#pragma warning disable SA1116 // Split parameters should start on line after declaration. Reviewed;
    [HarmonyPatch(nameof(TailoringMenu.CraftItem))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            { // find right_item.HasContextTag("color_prismatic");
                new(SpecialCodeInstructionCases.LdArg),
                new(OpCodes.Ldstr, "color_prismatic"),
                new(OpCodes.Callvirt, typeof(Item).GetCachedMethod(nameof(Item.HasContextTag), ReflectionCache.FlagTypes.InstanceFlags)),
                new(SpecialCodeInstructionCases.Wildcard, (instr) => instr.opcode == OpCodes.Brfalse || instr.opcode == OpCodes.Brfalse_S),
            })
            .Push()
            .Advance(3)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .DefineAndAttachLabel(out Label label)
            .Pop();

            CodeInstruction? arg = helper.CurrentInstruction.Clone();

            helper.GetLabels(out IList<Label>? labelsToMove)
            .Insert(new CodeInstruction[]
            { // and insert if (right_item.QualifiedItemId == "(O)74" )
                arg,
                new(OpCodes.Callvirt, typeof(Item).GetCachedProperty(nameof(Item.QualifiedItemId), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
                new(OpCodes.Ldstr, "(O)74"), // prismatic shard
                new(OpCodes.Call, typeof(string).GetCachedMethod("op_Equality", ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Brtrue, label),
            }, withLabels: labelsToMove);

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
#pragma warning restore SA1116 // Split parameters should start on line after declaration
}
