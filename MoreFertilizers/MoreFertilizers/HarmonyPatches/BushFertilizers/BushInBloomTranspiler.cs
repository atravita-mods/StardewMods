using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Reflection;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using MoreFertilizers.Framework;
using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.BushFertilizers;

/// <summary>
/// Holds the transpiler that adjusts Bush.inBloom.
/// </summary>
[HarmonyPatch(typeof(Bush))]
internal static class BushInBloomTranspiler
{
    private static int ReplaceSeasonForTeaBushes(int prevValue, Bush? bush)
        => bush?.modData?.GetBool(CanPlaceHandler.BountifulBush) == true ? 15 : prevValue;

    private static int ReplaceStartSpring(int prevValue, Bush? bush)
        => bush?.modData?.GetBool(CanPlaceHandler.BountifulBush) == true ? 13 : prevValue;

    private static int ReplaceEndSpring(int prevValue, Bush? bush)
        => bush?.modData?.GetBool(CanPlaceHandler.BountifulBush) == true ? 21 : prevValue;

    private static int ReplaceStartFall(int prevValue, Bush? bush)
        => bush?.modData?.GetBool(CanPlaceHandler.BountifulBush) == true ? 6 : prevValue;

    private static int ReplaceEndFall(int prevValue, Bush? bush)
        => bush?.modData?.GetBool(CanPlaceHandler.BountifulBush) == true ? 14 : prevValue;

    [HarmonyPatch(nameof(Bush.inBloom))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Call, typeof(Bush).InstanceMethodNamed(nameof(Bush.getAge))),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldc_I4_S, 22),
            })
            .Advance(1)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(BushInBloomTranspiler).StaticMethodNamed(nameof(ReplaceSeasonForTeaBushes))),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldc_I4_S, 14),
            })
            .Advance(1)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(BushInBloomTranspiler).StaticMethodNamed(nameof(ReplaceStartSpring))),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldc_I4_S, 19),
            })
            .Advance(1)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(BushInBloomTranspiler).StaticMethodNamed(nameof(ReplaceEndSpring))),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldc_I4_7),
            })
            .Advance(1)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(BushInBloomTranspiler).StaticMethodNamed(nameof(ReplaceStartFall))),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldc_I4_S, 12),
            })
            .Advance(1)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(BushInBloomTranspiler).StaticMethodNamed(nameof(ReplaceEndFall))),
            });

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling Hoedirt.Draw:\n\n{ex}", LogLevel.Error);
        }
        return null;
    }
}