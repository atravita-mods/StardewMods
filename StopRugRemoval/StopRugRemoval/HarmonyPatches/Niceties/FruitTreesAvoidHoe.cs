using System.Reflection;
using System.Reflection.Emit;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Fruit trees aren't damage by hoes.
/// </summary>
[HarmonyPatch(typeof(FruitTree))]
internal static class FruitTreesAvoidHoe
{
    [HarmonyPatch(nameof(FruitTree.performToolAction))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            _ = helper.FindNext(
            [
                new(OpCodes.Brtrue_S),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Isinst, typeof(Hoe)),
                new(OpCodes.Brtrue_S),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Isinst, typeof(MeleeWeapon)),
            ])
            .Remove(6)
            .FindNext(
            [
                new(OpCodes.Brtrue_S),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Callvirt, typeof(Tool).GetCachedProperty(nameof(Tool.BaseName), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
                new(OpCodes.Ldstr, "Hoe"),
                new(OpCodes.Callvirt, typeof(string).GetCachedMethod<string>(nameof(string.Contains), ReflectionCache.FlagTypes.InstanceFlags)),
                new(OpCodes.Brfalse),
            ])
            .Remove(5);

            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}