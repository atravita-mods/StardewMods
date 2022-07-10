using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Extensions;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using StardewValley.Minigames;

namespace EasierDartPuzzle.HarmonyPatches;

/// <summary>
/// Adjusts how much the darts shake.
/// </summary>
[HarmonyPatch(typeof(Darts))]
internal static class AdjustDartsJitter
{
    private static float GetDartsJitterAdjustment()
        => ModEntry.Config.JitterMultiplier;

    [HarmonyPatch(nameof(Darts.tick))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            helper.ForEachMatch(
                new CodeInstructionWrapper[]
                {
                    new(OpCodes.Mul),
                    new(OpCodes.Call, typeof(Math).GetCachedMethod(nameof(Math.Sin), ReflectionCache.FlagTypes.StaticFlags)),
                },
                (helper) =>
                {
                    helper.Advance(1)
                    .Insert(new CodeInstruction[]
                    {
                        new(OpCodes.Call, typeof(AdjustDartsJitter).GetCachedMethod(nameof(GetDartsJitterAdjustment), ReflectionCache.FlagTypes.StaticFlags)),
                        new(OpCodes.Div),
                    });
                    return true;
                },
                maxCount: 2);

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into error transpiling {original.GetFullName()}!\n\n{ex}", LogLevel.Error);
            original?.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}