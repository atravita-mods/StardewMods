using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Reflection;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using StardewValley.Locations;

namespace EasierDartPuzzle.HarmonyPatches;

/// <summary>
/// Transpiler that adjusts the number of darts you get.
/// </summary>
[HarmonyPatch(typeof(IslandSouthEastCave))]
internal static class AdjustDartNumberTranspiler
{
    private static int GetMinimumDartNumber()
        => Math.Min(ModEntry.Config.MinDartCount, ModEntry.Config.MaxDartCount);

    private static int GetMaximumDartNumber()
        => Math.Max(ModEntry.Config.MinDartCount, ModEntry.Config.MaxDartCount);

    private static int GetMidddleDartNumber()
        => (ModEntry.Config.MinDartCount + ModEntry.Config.MaxDartCount) / 2;

    [HarmonyPatch(nameof(IslandSouthEastCave.answerDialogueAction))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            { // find team.GetDroppedLimitNutCount("Darts");
                new(OpCodes.Ldstr, "Darts"),
                new(OpCodes.Callvirt, typeof(FarmerTeam).GetCachedMethod(nameof(FarmerTeam.GetDroppedLimitedNutCount), ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldc_I4_S, 20),
            })
            .ReplaceInstruction(OpCodes.Call, typeof(AdjustDartNumberTranspiler).StaticMethodNamed(nameof(GetMaximumDartNumber)), keepLabels: true)
            .FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldc_I4_S, 15),
            })
            .ReplaceInstruction(OpCodes.Call, typeof(AdjustDartNumberTranspiler).StaticMethodNamed(nameof(GetMidddleDartNumber)), keepLabels: true)
            .FindNext(new CodeInstructionWrapper[]
             {
                 new(OpCodes.Ldc_I4_S, 10),
             })
            .ReplaceInstruction(OpCodes.Call, typeof(AdjustDartNumberTranspiler).StaticMethodNamed(nameof(GetMinimumDartNumber)), keepLabels: true);

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into error transpiling dart count!\n\n{ex}", LogLevel.Error);
            original?.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}
