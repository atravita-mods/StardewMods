using System.Reflection;
using System.Reflection.Emit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using StardewValley.Buildings;

namespace DrawFishPondsOverGrass.HarmonyPatches;

/// <summary>
/// Patch that handles drawing jumping fish a little forward.
/// </summary>
[HarmonyPatch]
internal static class JumpingFishLayerDepthFix
{
    private const float Offset = 280f;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return typeof(JumpingFish).GetCachedMethod(nameof(JumpingFish.Draw), ReflectionCache.FlagTypes.InstanceFlags);
        yield return typeof(PondFishSilhouette).GetCachedMethod(nameof(PondFishSilhouette.Draw), ReflectionCache.FlagTypes.InstanceFlags);
    }

    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.ForEachMatch(
                new CodeInstructionWrapper[]
                {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldflda),
                    new(OpCodes.Ldfld),
                    new(OpCodes.Ldc_R4, 10000f),
                    new(OpCodes.Div),
                },
                transformer: static (helper) =>
                {
                    helper.Advance(3)
                        .Insert(new CodeInstruction[]
                        {
                            new (OpCodes.Ldc_R4, Offset),
                            new (OpCodes.Add),
                        });
                    return true;
                });
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}