﻿using System.Reflection;
using System.Reflection.Emit;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Locations;

namespace MoreFertilizers.HarmonyPatches.FishFood;

/// <summary>
/// A transpiler against Caldera.getFish.
/// </summary>
[HarmonyPatch(typeof(Caldera))]
internal static class CalderaGetFishTranspiler
{
    [HarmonyPatch(nameof(Caldera.getFish))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            { // advance past the location where the painting is given.
                new(OpCodes.Call, typeof(Vector2).GetCachedProperty(nameof(Vector2.Zero), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod()),
                new(OpCodes.Newobj),
                new(OpCodes.Ret),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldsfld, typeof(Game1).GetCachedField(nameof(Game1.random), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Callvirt, typeof(Random).GetCachedMethod(nameof(Random.NextDouble), ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                new(SpecialCodeInstructionCases.LdArg),
                new(OpCodes.Conv_R8),
                new(OpCodes.Mul),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Mul),
            })
            .Advance(1)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(GetFishTranspiler).GetCachedMethod(nameof(GetFishTranspiler.AlterFishChance), ReflectionCache.FlagTypes.StaticFlags)),
            });

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling Caldera.GetFish:\n\n{ex}", LogLevel.Error);
            original?.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}
