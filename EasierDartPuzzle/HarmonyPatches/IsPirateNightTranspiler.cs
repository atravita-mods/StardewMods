using System.Reflection;
using System.Reflection.Emit;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
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
        => Context.IsMultiplayer ? ModEntry.Config.MPPirateArrivalTime : 2000;

    [HarmonyPatch(nameof(IslandSouthEastCave.isPirateNight))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindFirst(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldsfld, typeof(Game1).GetCachedField(nameof(Game1.timeOfDay), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Ldc_I4, 2000),
                new(OpCodes.Blt_S),
            })
            .Advance(1)
            .ReplaceInstruction(OpCodes.Call, typeof(IsPirateNightTranspiler).GetCachedMethod(nameof(IsPirateNightTranspiler.GetPirateArrivalTime), ReflectionCache.FlagTypes.StaticFlags), keepLabels: true);

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