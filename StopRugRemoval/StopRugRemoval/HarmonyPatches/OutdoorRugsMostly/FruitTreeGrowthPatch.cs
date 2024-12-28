using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Reflection;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace StopRugRemoval.HarmonyPatches.OutdoorRugsMostly;

/// <summary>
/// Patches against fruit trees to allow for growth near rugs.
/// </summary>
[HarmonyPatch(typeof(FruitTree))]
internal static class FruitTreeGrowthPatch
{
    /***************************************************
    * Removing rugs from the possible check list by
    * adding CollisionMask.Furniture to the IgnorePassables
    * of environment.IsTileOccupiedBy
    ****************************************************/

    [HarmonyPatch(nameof(FruitTree.IsGrowthBlocked))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindLast(
                    [
                    (OpCodes.Ldc_I4, 153),
                    OpCodes.Ldc_I4_0,
                    OpCodes.Ldc_I4_0,
                    (OpCodes.Callvirt, typeof(GameLocation).GetCachedMethod<Vector2, CollisionMask, CollisionMask, bool>(nameof(GameLocation.IsTileOccupiedBy), ReflectionCache.FlagTypes.InstanceFlags)),
                    ])
            .Advance(1)
            .ReplaceInstruction(new(OpCodes.Ldc_I4, (int)CollisionMask.Furniture));
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}