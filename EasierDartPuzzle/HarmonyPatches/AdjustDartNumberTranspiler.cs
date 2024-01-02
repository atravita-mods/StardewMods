namespace EasierDartPuzzle.HarmonyPatches;

using System.Reflection;
using System.Reflection.Emit;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using StardewValley.Locations;
using StardewValley.Minigames;

/// <summary>
/// Transpiler that adjusts the number of darts you get.
/// </summary>
[HarmonyPatch]
internal static class AdjustDartNumberTranspiler
{
    /// <summary>
    /// Gets the methods to patch.
    /// </summary>
    /// <returns>methods to patch.</returns>
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        yield return typeof(IslandSouthEastCave).GetCachedMethod(nameof(IslandSouthEastCave.answerDialogueAction), ReflectionCache.FlagTypes.InstanceFlags);
        yield return typeof(Darts).GetCachedMethod(nameof(Darts.QuitGame), ReflectionCache.FlagTypes.InstanceFlags);
    }

    /// <summary>
    /// Gets the minimum dart number.
    /// </summary>
    /// <returns>Minimum dart number.</returns>
    internal static int GetMinimumDartNumber()
        => Math.Min(ModEntry.Config.MinDartCount, ModEntry.Config.MaxDartCount);

    /// <summary>
    /// Gets the max dart number.
    /// </summary>
    /// <returns>Max dart number.</returns>
    internal static int GetMaximumDartNumber()
        => Math.Max(ModEntry.Config.MinDartCount, ModEntry.Config.MaxDartCount);

    /// <summary>
    /// Gets the middle dart number.
    /// </summary>
    /// <returns>Middle dart number.</returns>
    internal static int GetMidddleDartNumber()
        => (ModEntry.Config.MinDartCount + ModEntry.Config.MaxDartCount) / 2;

    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            { // find team.GetDroppedLimitNutCount("Darts");
                new(OpCodes.Ldstr, "Darts"),
                new(OpCodes.Callvirt, typeof(FarmerTeam).GetCachedMethod(nameof(FarmerTeam.GetDroppedLimitedNutCount), ReflectionCache.FlagTypes.InstanceFlags)),
            });

            int pointer = helper.Pointer;
            helper.FindNext(
            [
                new(OpCodes.Ldc_I4_S, 15),
            ])
            .ReplaceInstruction(OpCodes.Call, typeof(AdjustDartNumberTranspiler).GetCachedMethod(nameof(GetMidddleDartNumber), ReflectionCache.FlagTypes.StaticFlags), keepLabels: true)
            .JumpTo(pointer)
            .FindNext(new CodeInstructionWrapper[]
             {
                 new(OpCodes.Ldc_I4_S, 10),
             })
            .ReplaceInstruction(OpCodes.Call, typeof(AdjustDartNumberTranspiler).GetCachedMethod(nameof(GetMinimumDartNumber), ReflectionCache.FlagTypes.StaticFlags), keepLabels: true)
            .JumpTo(pointer)
            .FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldc_I4_S, 20),
            })
            .ReplaceInstruction(OpCodes.Call, typeof(AdjustDartNumberTranspiler).GetCachedMethod(nameof(GetMaximumDartNumber), ReflectionCache.FlagTypes.StaticFlags), keepLabels: true);

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
