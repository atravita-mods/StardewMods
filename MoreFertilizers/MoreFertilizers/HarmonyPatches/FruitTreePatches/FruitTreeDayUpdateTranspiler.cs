﻿using System.Reflection;
using System.Reflection.Emit;

using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;

using AtraCore;
using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using MoreFertilizers.Framework;

using Netcode;

using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.FruitTreePatches;

/// <summary>
/// Transpiles DayUpdate to cause fruit trees to grow up faster.
/// </summary>
[HarmonyPatch(typeof(FruitTree))]
internal static class FruitTreeDayUpdateTranspiler
{
    /// <summary>
    /// Applies the fruit tree update patch to DGA too.
    /// </summary>
    /// <param name="harmony">Harmony instance.</param>
    internal static void ApplyDGAPatch(Harmony harmony)
    {
        try
        {
            Type dgaFruitTree = AccessTools.TypeByName("DynamicGameAssets.Game.CustomFruitTree")
                ?? ReflectionThrowHelper.ThrowMethodNotFoundException<Type>("DGA Fruit Trees");
            harmony.Patch(
                original: dgaFruitTree.GetCachedMethod("dayUpdate", ReflectionCache.FlagTypes.InstanceFlags),
                transpiler: new HarmonyMethod(typeof(FruitTreeDayUpdateTranspiler), nameof(Transpiler)));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("transpiling DGA", ex);
        }
    }

    private static int CalculateExtraGrowth(FruitTree tree)
    {
        try
        {
            if (tree.modData?.GetInt(CanPlaceHandler.FruitTreeFertilizer) is int result
                && Singletons.Random.OfChance(0.15 * result))
            {
                ModEntry.ModMonitor.LogIfVerbose(() => $"Speeding up fruit tree at {tree.currentTileLocation}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"calculating extra growth for fruit trees", ex);
        }
        return 0;
    }

    [HarmonyPatch(nameof(FruitTree.dayUpdate))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new (SpecialCodeInstructionCases.LdArg),
                new (SpecialCodeInstructionCases.LdArg),
                new (OpCodes.Call, typeof(FruitTree).GetCachedMethod(nameof(FruitTree.IsGrowthBlocked), ReflectionCache.FlagTypes.StaticFlags)),
                new (SpecialCodeInstructionCases.StLoc),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                new (OpCodes.Ldarg_0),
                new (OpCodes.Ldfld, typeof(FruitTree).GetCachedField(nameof(FruitTree.daysUntilMature), ReflectionCache.FlagTypes.InstanceFlags)),
                new (OpCodes.Dup),
                new (OpCodes.Callvirt, typeof(NetFieldBase<int, NetInt>).GetCachedProperty("Value", ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
                new (SpecialCodeInstructionCases.StLoc),
                new (SpecialCodeInstructionCases.LdLoc),
                new (OpCodes.Ldc_I4_1),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                new (OpCodes.Ldc_I4_1),
            })
            .Advance(1)
            .Insert(new CodeInstruction[]
            {
                new (OpCodes.Ldarg_0),
                new (OpCodes.Call, typeof(FruitTreeDayUpdateTranspiler).GetCachedMethod(nameof(CalculateExtraGrowth), ReflectionCache.FlagTypes.StaticFlags)),
                new (OpCodes.Add),
            });
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}