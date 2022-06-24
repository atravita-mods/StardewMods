using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Reflection;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using StardewValley.Locations;

namespace EasierDartPuzzle.HarmonyPatches;

/// <summary>
/// Transpiles IsPirateNight to make it earlier in multiplayer.
/// </summary>
[HarmonyPatch(typeof(IslandSouthEastCave))]
internal static class IsPirateNightTranspiler
{
    private static int GetPirateArrivalTime()
        => Context.IsMultiplayer ? 1600 : 2000;

    [HarmonyPatch(nameof(IslandSouthEastCave.isPirateNight))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindFirst(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldsfld, typeof(Game1).StaticFieldNamed(nameof(Game1.timeOfDay))),
                new(OpCodes.Ldc_I4, 2000),
                new(OpCodes.Blt_S),
            })
            .Advance(1)
            .ReplaceInstruction(OpCodes.Call, typeof(IsPirateNightTranspiler).StaticMethodNamed(nameof(IsPirateNightTranspiler.GetPirateArrivalTime)), keepLabels: true);

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into error transpiling pirates to arrive earlier in multiplayer!\n\n{ex}", LogLevel.Error);
        }
        return null;
    }
}