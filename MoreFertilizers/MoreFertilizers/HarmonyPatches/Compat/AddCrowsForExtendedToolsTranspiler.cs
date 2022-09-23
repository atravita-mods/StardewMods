using System.Reflection;
using System.Reflection.Emit;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using Microsoft.Xna.Framework;

namespace MoreFertilizers.HarmonyPatches.Compat;

#warning - remove in Stardew 1.6

/// <summary>
/// Applies a transpiler to make Prismatic and Radioactive's sprinklers work as anti-crow devices.
/// </summary>
internal static class AddCrowsForExtendedToolsTranspiler
{
    internal static void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.Patch(
                original: typeof(Farm).GetCachedMethod(nameof(Farm.addCrows), ReflectionCache.FlagTypes.InstanceFlags),
                transpiler: new HarmonyMethod(typeof(AddCrowsForExtendedToolsTranspiler), nameof(Transpiler)));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed in attempting to transpile Farm.AddCrows\n\n{ex}", LogLevel.Error);
        }
    }

    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldloca),
                new(OpCodes.Call, typeof(KeyValuePair<Vector2, SObject>).GetCachedProperty("Value", ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
                new(OpCodes.Ldfld, typeof(SObject).GetCachedField(nameof(SObject.bigCraftable), ReflectionCache.FlagTypes.InstanceFlags)),
                new(OpCodes.Call),
                new(SpecialCodeInstructionCases.Wildcard, (instr) => instr.opcode == OpCodes.Brfalse || instr.opcode == OpCodes.Brfalse_S),
            })
            .GetLabels(out var labels)
            .Remove(5)
            .AttachLabel(labels.ToArray());

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling {original.FullDescription()}:\n\n{ex}", LogLevel.Error);
            original?.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}
