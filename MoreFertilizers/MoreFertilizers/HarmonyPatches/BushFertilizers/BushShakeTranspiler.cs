using System.Reflection;
using System.Reflection.Emit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using MoreFertilizers.Framework;

using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.BushFertilizers;

/// <summary>
/// Transpiles Bush.shake to add a beverage sometimes....
/// </summary>
[HarmonyPatch(typeof(Bush))]
internal static class BushShakeTranspiler
{
    private static void GenerateBeverage(Bush bush, int index)
    {
        if (bush?.modData?.GetBool(CanPlaceHandler.MiraculousBeverages) == true && MiraculousFertilizerHandler.GetBeverage(index) is SObject output)
        {
            Game1.createItemDebris(output, bush.tilePosition.Value * 64f, -1);
        }
    }

    [HarmonyPatch("shake")]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldstr, "spring"),
            })
            .FindNext(new CodeInstructionWrapper[]
            { // if (shakeOff == -1)
                new(SpecialCodeInstructionCases.LdLoc),
                new(OpCodes.Ldfld),
                new(OpCodes.Ldc_I4_M1),
                new(OpCodes.Beq),
            })
            .Copy(2, out IEnumerable<CodeInstruction>? copy)
            .GetLabels(out IList<Label> labels);

            List<CodeInstruction> codes = new() { new(OpCodes.Ldarg_0) };
            codes.AddRange(copy);
            codes.Add(new(OpCodes.Call, typeof(BushShakeTranspiler).GetCachedMethod(nameof(GenerateBeverage), ReflectionCache.FlagTypes.StaticFlags)));

            helper.Insert(codes.ToArray(), labels);

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