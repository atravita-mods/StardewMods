using System.Reflection;
using System.Reflection.Emit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using StardewValley.BellsAndWhistles;
using StardewValley.TerrainFeatures;

namespace CritterRings.HarmonyPatches.BunnyRing;

/// <summary>
/// A transpiler to prevent rabbits from harvesting walnuts.
/// </summary>
[HarmonyPatch(typeof(Rabbit))]
internal static class RabbitTranspiler
{
    private static bool IsWalnutInBloom(Bush bush)
        => bush is not null && bush.size.Value == Bush.walnutBush && bush.tileSheetOffset.Value == 1;

    [HarmonyPatch("update")]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = StyleCopConstants.SplitParametersIntentional)]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                SpecialCodeInstructionCases.LdLoc,
                (OpCodes.Isinst, typeof(Bush)),
                SpecialCodeInstructionCases.StLoc,
                SpecialCodeInstructionCases.LdLoc,
                OpCodes.Brfalse_S,
            })
            .Push()
            .Advance(2);

            CodeInstruction loc = helper.CurrentInstruction.ToLdLoc();

            helper
            .Advance(2)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .DefineAndAttachLabel(out Label jumpPoint)
            .Pop()
            .GetLabels(out IList<Label>? labelsToMove)
            .Insert(new CodeInstruction[]
            {
                loc,
                new(OpCodes.Call, typeof(RabbitTranspiler).GetCachedMethod(nameof(IsWalnutInBloom), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Brtrue, jumpPoint),
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
}
