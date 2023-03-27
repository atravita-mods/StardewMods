using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;
using AtraShared.Utils.Extensions;
using StardewValley.Tools;
using GrowableGiantCrops.Framework;

namespace GrowableGiantCrops.HarmonyPatches.ToolPatches;

[HarmonyPatch(typeof(GameLocation))]
internal static class ShovelUpgradablePatch
{
    [HarmonyPatch(nameof(GameLocation.blacksmith))]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = "Reviewed.")]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                SpecialCodeInstructionCases.LdLoc,
                (OpCodes.Isinst, typeof(GenericTool)),
                OpCodes.Brfalse_S,
            })
            .Push()
            .Advance(3)
            .DefineAndAttachLabel(out var jumpPoint)
            .Pop();

            var ldloc = helper.CurrentInstruction.Clone();
            helper.GetLabels(out var labelsToMove)
            .Insert(new CodeInstruction[]
            {
                ldloc,
                new(OpCodes.Isinst, typeof(ShovelTool)),
                new(OpCodes.Brtrue, jumpPoint),
            }, labelsToMove);

            // helper.Print();
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
