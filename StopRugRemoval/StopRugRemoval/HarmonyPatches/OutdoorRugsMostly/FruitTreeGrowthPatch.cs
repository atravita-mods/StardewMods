using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Reflection;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
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
    * Removing rugs from the possible check list.
    * Original method: if (o == null) { return true;}
    * New methods: if (o == null || (o is Furniture f && f.furniture_type.Value == Furniture.rug)) { return true;}
    ****************************************************/

    [HarmonyPatch(nameof(FruitTree.IsGrowthBlocked))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindLast(
                    [
                    new (OpCodes.Ldc_I4_0),
                    new (OpCodes.Callvirt, typeof(GameLocation).GetCachedMethod<int, int, bool>(nameof(GameLocation.getObjectAtTile), ReflectionCache.FlagTypes.InstanceFlags)),
                    ])
                .FindNext(
                    [
                    new (SpecialCodeInstructionCases.StLoc, typeof(SObject)),
                    new (SpecialCodeInstructionCases.LdLoc, typeof(SObject)),
                    new (OpCodes.Brfalse_S),
                    ])
                .Advance(2)
                .Insert(
                    [
                    new (OpCodes.Call, typeof(FruitTreeGrowthPatch).StaticMethodNamed(nameof(IsNotRugOrNull))),
                    ]);
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }

    private static bool IsNotRugOrNull(SObject? obj)
        => obj is not null && (obj is not Furniture f || f.furniture_type.Value != Furniture.rug);
}