using System.Reflection;
using System.Reflection.Emit;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.HarmonyPatches.GrassPatches;

/// <summary>
/// Patches on grass.
/// </summary>
[HarmonyPatch(typeof(Grass))]
internal static class GrassPatches
{
    [HarmonyPatch(nameof(Grass.dayUpdate))]
    private static bool Prefix(Grass __instance)
        => __instance is null || !(SObjectPatches.IsMoreGrassGrass?.Invoke(__instance) == true);

    [HarmonyPatch(nameof(Grass.dayUpdate))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            { // find and remove this.grassType == 1. We want all grass to grow!
                OpCodes.Ldarg_0,
                (OpCodes.Ldfld, typeof(Grass).GetCachedField(nameof(Grass.grassType), ReflectionCache.FlagTypes.InstanceFlags)),
                OpCodes.Call,
                OpCodes.Ldc_I4_1,
                OpCodes.Bne_Un_S,
            })
            .GetLabels(out IList<Label>? labelsToMove)
            .Remove(5)
            .AttachLabels(labelsToMove);

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
