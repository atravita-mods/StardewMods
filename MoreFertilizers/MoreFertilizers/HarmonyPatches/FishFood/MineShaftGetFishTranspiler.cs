﻿using System.Reflection;
using System.Reflection.Emit;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using StardewValley.Locations;

namespace MoreFertilizers.HarmonyPatches.FishFood;

/// <summary>
/// Holds the transpiler against MineShaft's getFish to adjust mine fish chances.
/// </summary>
[HarmonyPatch(typeof(MineShaft))]
internal static class MineShaftGetFishTranspiler
{
#pragma warning disable SA1116 // Split parameters should start on line after declaration
    [HarmonyPatch(nameof(MineShaft.getFish))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        // Being a little lazy here.
        // Adjusting for each (Game1.random.NextDouble() < double + double * chanceMultiplier)
        // Which is repeated three times.
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            // looking for three occurances of Game1.random.NextDouble() in that switch case.
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldc_I4_M1),
                new(OpCodes.Call, typeof(MineShaft).GetCachedMethod(nameof(MineShaft.getMineArea), ReflectionCache.FlagTypes.InstanceFlags)),
            });

            int startindex = helper.Pointer;

            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(SpecialCodeInstructionCases.LdArg),
                new(OpCodes.Callvirt, typeof(Farmer).GetCachedProperty(nameof(Farmer.FishingLevel), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
            });

            int endindex = helper.Pointer + 4; // since I'm adding instructions, the end point will move up.

            helper.ForEachMatch(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldsfld, typeof(Game1).GetCachedField(nameof(Game1.random), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Callvirt, typeof(Random).GetCachedMethod(nameof(Random.NextDouble), ReflectionCache.FlagTypes.InstanceFlags)),
            },
            transformer: (helper) =>
            {
                helper.FindNext(new CodeInstructionWrapper[]
                {
                    new(SpecialCodeInstructionCases.LdLoc),
                    new(OpCodes.Mul),
                    new(OpCodes.Add),
                    new(OpCodes.Bge_Un_S),
                })
                .Advance(3)
                .Insert(new CodeInstruction[]
                {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Call, typeof(GetFishTranspiler).GetCachedMethod(nameof(GetFishTranspiler.AlterFishChance), ReflectionCache.FlagTypes.StaticFlags)),
                });
                return true;
            },
            startindex: startindex,
            intendedendindex: endindex,
            maxCount: 3);

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling MineShaft.GetFish:\n\n{ex}", LogLevel.Error);
            original?.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
#pragma warning restore SA1116 // Split parameters should start on line after declaration
}
