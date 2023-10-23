using System.Reflection;
using System.Reflection.Emit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using GrowableGiantCrops.Framework;

using HarmonyLib;

using StardewValley.Tools;

namespace GrowableGiantCrops.HarmonyPatches.ItemPatches;

/// <summary>
/// Transpiler to allow the shovel to also affect artifact spots as if it was a hoe.
/// </summary>
[HarmonyPatch(typeof(SObject))]
internal static class ArtifactSpotTranspiler
{
    [HarmonyPatch(nameof(SObject.performToolAction))]
    [SuppressMessage("SMAPI.CommonErrors", "AvoidNetField:Avoid Netcode types when possible", Justification = StyleCopConstants.UsedForMatchingOnly)]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            { // this.parentSheetIndex.Value == 590
                OpCodes.Ldarg_0,
                (OpCodes.Ldfld, typeof(Item).GetCachedField(nameof(Item.parentSheetIndex), ReflectionCache.FlagTypes.InstanceFlags)),
                OpCodes.Call,
                (OpCodes.Ldc_I4, 590),
            })
            .FindNext(new CodeInstructionWrapper[]
            { // t is Hoe
                SpecialCodeInstructionCases.LdLoc,
                OpCodes.Ldfld,
                (OpCodes.Isinst, typeof(Hoe)),
                OpCodes.Brfalse,
            })
            .Push()
            .Advance(4)
            .DefineAndAttachLabel(out Label jumpPoint)
            .Pop()
            .GetLabels(out IList<Label>? labelsToMove)
            .Copy(4, out IEnumerable<CodeInstruction>? codes);

            // insert t is ShovelTool -> final is if (t is ShovelTool || t is Hoe);
            CodeInstruction[] copy = codes.ToArray();
            copy[2].operand = typeof(ShovelTool);
            copy[3] = new (OpCodes.Brtrue, jumpPoint);

            helper.Insert(copy, labelsToMove);

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}
