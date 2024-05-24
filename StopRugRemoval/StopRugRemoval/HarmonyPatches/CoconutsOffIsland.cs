﻿using System.Reflection;
using System.Reflection.Emit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace StopRugRemoval.HarmonyPatches;

/// <summary>
/// Class that holds patch to allow jukeboxes to be played anywhere.
/// </summary>
[HarmonyPatch(typeof(Tree))]
internal static class CoconutsOffIsland
{
    /// <summary>
    /// Whether or not jukeboxes should be playable at this location.
    /// </summary>
    /// <param name="location">Gamelocation.</param>
    /// <returns>whether the jukebox should be playable.</returns>
    public static bool AllowCoconutShakingHere(GameLocation location)
        => location is IslandLocation
            || (ModEntry.Config.Enabled && ModEntry.Config.GoldenCoconutsOffIsland && Game1.netWorldState.Value.GoldenCoconutCracked.Value);

    [HarmonyPatch("shake")]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                SpecialCodeInstructionCases.LdArg,
                OpCodes.Brfalse_S,
                SpecialCodeInstructionCases.LdArg,
                OpCodes.Isinst,
                OpCodes.Brfalse_S,
            })
            .Advance(3)
            .ReplaceInstruction(
                opcode: OpCodes.Call,
                operand: typeof(CoconutsOffIsland).GetCachedMethod(nameof(AllowCoconutShakingHere), ReflectionCache.FlagTypes.StaticFlags),
                keepLabels: true);
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}