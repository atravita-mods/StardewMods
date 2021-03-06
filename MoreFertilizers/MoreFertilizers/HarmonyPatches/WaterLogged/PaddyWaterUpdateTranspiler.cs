using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Reflection;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.WaterLogged;

/// <summary>
/// Transpiler to update neighbors if a paddy crop is fertilized with the waterlogged fertilizer.
/// </summary>
[HarmonyPatch(typeof(HoeDirt))]
internal static class PaddyWaterUpdateTranspiler
{
    private static void UpdateNeighbors(HoeDirt dirt, int tileX, int tileY, GameLocation location)
    {
        Vector2 v = new(tileX, tileY);
        dirt.nearWaterForPaddy.Value = -1;
        if (dirt.hasPaddyCrop() && dirt.paddyWaterCheck(location, v))
        {
            dirt.state.Value = -1;
            dirt.updateNeighbors(location, v);
        }
    }

    [HarmonyPatch(nameof(HoeDirt.plant))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_S),
                new(OpCodes.Call, typeof(HoeDirt).GetCachedMethod("applySpeedIncreases", ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .Advance(3)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_2),
                new(OpCodes.Ldarg_3),
                new(OpCodes.Ldarg_S, 6),
                new(OpCodes.Call, typeof(PaddyWaterUpdateTranspiler).StaticMethodNamed(nameof(UpdateNeighbors))),
            });
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling Hoedirt.plant:\n\n{ex}", LogLevel.Error);
        }
        return null;
    }
}