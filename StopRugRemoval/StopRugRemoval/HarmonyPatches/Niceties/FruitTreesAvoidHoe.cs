using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Reflection;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace StopRugRemoval.HarmonyPatches.Niceties;

[HarmonyPatch(typeof(FruitTree))]
internal static class FruitTreesAvoidHoe
{
    [HarmonyPatch(nameof(FruitTree.performToolAction))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldarg_1),
                new(OpCodes.Isinst, typeof(Hoe)),
                new(OpCodes.Brtrue_S),
            })
            .Remove(3)
            .FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldarg_1),
                new(OpCodes.Callvirt, typeof(Tool).InstancePropertyNamed(nameof(Tool.BaseName)).GetGetMethod()),
                new(OpCodes.Ldstr, "Hoe"),
                new(OpCodes.Callvirt, typeof(string).InstanceMethodNamed(nameof(string.Contains), new Type[] { typeof(string) })),
                new(OpCodes.Brfalse_S),
            })
            .Advance(-1)
            .Remove(5);
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into error transpiling fruit trees to avoid hoe damage.\n\n{ex}", LogLevel.Error);
        }
        return null;
    }
}