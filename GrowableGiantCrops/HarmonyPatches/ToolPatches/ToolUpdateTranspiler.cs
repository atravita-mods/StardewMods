using System.Reflection;
using System.Reflection.Emit;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using GrowableGiantCrops.Framework;
using HarmonyLib;
using StardewValley.Tools;

namespace GrowableGiantCrops.HarmonyPatches.ToolPatches;

[HarmonyPatch(typeof(Tool))]
internal static class ToolUpdateTranspiler
{
    [HarmonyPatch(nameof(Tool.Update))]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = "Reviewed.")]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        return null;
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

            helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling {original.FullDescription()}:\n\n{ex}", LogLevel.Error);
            original.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}
