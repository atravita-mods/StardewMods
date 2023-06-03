using System.Reflection;
using System.Reflection.Emit;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using GrowableGiantCrops.Framework;

using HarmonyLib;

using StardewValley.Tools;

namespace GrowableGiantCrops.HarmonyPatches.ToolPatches;

/// <summary>
/// Patches on tools.
/// </summary>
[HarmonyPatch(typeof(Tool))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class PatchesOnTool
{
    [HarmonyPatch(nameof(Tool.isHeavyHitter))]
    private static bool Prefix(Tool __instance, ref bool __result)
    {
        if (__instance is ShovelTool)
        {
            __result = true;
            return false;
        }
        return true;
    }

    [HarmonyPatch(nameof(Tool.Update))]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = StyleCopConstants.SplitParametersIntentional)]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                OpCodes.Ldarg_0,
                (OpCodes.Isinst, typeof(WateringCan)),
                OpCodes.Brfalse_S,
            })
            .Push()
            .Advance(3)
            .DefineAndAttachLabel(out Label jumpPoint)
            .Pop()
            .GetLabels(out IList<Label>? labelsToMove)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
                new (OpCodes.Isinst, typeof(ShovelTool)),
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
