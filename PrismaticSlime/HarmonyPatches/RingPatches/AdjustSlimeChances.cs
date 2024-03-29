﻿using System.Reflection;
using System.Reflection.Emit;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using StardewValley.Locations;
using StardewValley.Monsters;

namespace PrismaticSlime.HarmonyPatches.RingPatches;

/// <summary>
/// Adjusts the chances of the prismatic slime spawning in the MineShaft.
/// </summary>
[HarmonyPatch(typeof(MineShaft))]
internal static class AdjustSlimeChances
{
    private static double AdjustChanceForPrismaticRing(double chance, Farmer player)
    {
        if (ModEntry.PrismaticSlimeRing == -1 || player is null)
        {
            return chance;
        }
        else if (player.isWearingRing(ModEntry.PrismaticSlimeRing))
        {
            return Math.Clamp(chance * 5, 0, 1);
        }
        return chance;
    }

    [HarmonyPatch("populateLevel")]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            helper.FindNext(new CodeInstructionWrapper[]
            { // (monster as GreenSlime).makePrismatic(),
                SpecialCodeInstructionCases.LdLoc,
                (OpCodes.Isinst, typeof(GreenSlime)),
                (OpCodes.Callvirt, typeof(GreenSlime).GetCachedMethod(nameof(GreenSlime.makePrismatic), ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .FindPrev(new CodeInstructionWrapper[]
            {
                (OpCodes.Ldsfld, typeof(Game1).GetCachedField(nameof(Game1.random), ReflectionCache.FlagTypes.StaticFlags)),
                (OpCodes.Callvirt, typeof(Random).GetCachedMethod(nameof(Random.NextDouble), ReflectionCache.FlagTypes.InstanceFlags, Type.EmptyTypes)),
                (OpCodes.Ldc_R8, 0.012),
            })
            .Advance(3)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Call, typeof(Game1).GetCachedProperty(nameof(Game1.player), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod()),
                new(OpCodes.Call, typeof(AdjustSlimeChances).GetCachedMethod(nameof(AdjustChanceForPrismaticRing), ReflectionCache.FlagTypes.StaticFlags)),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                (OpCodes.Ldstr, "Wizard2"),
                (OpCodes.Callvirt, typeof(FarmerTeam).GetCachedMethod<string>(nameof(FarmerTeam.SpecialOrderActive), ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .Advance(1)
            .ReplaceInstruction(
                opcode: OpCodes.Call,
                operand: typeof(FarmerTeamExtensions).GetCachedMethod(nameof(FarmerTeamExtensions.SpecialOrderActiveOrCompleted), ReflectionCache.FlagTypes.StaticFlags),
                keepLabels: true);

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into error transpiling {original.FullDescription()}.\n\n{ex}", LogLevel.Error);
            original.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}