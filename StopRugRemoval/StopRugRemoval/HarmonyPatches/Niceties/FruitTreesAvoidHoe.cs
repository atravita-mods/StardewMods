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
    /// <summary>
    /// Applies late patches.
    /// </summary>
    /// <param name="harmony">Harmony instance.</param>
    /// <param name="registry">Mod registry.</param>
    internal static void ApplyPatches(Harmony harmony, IModRegistry registry)
    {
        if (registry.Get("spacechase0.DynamicGameAssets") is null)
        {
            return;
        }
        try
        {
            if (AccessTools.TypeByName("DynamicGameAssets.Game.CustomFruitTree") is Type dgaTree)
            {
                ModEntry.ModMonitor.Log("Transpiling DGA to remove damage to fruit trees from hoes", LogLevel.Info);
                harmony.Patch(
                    original: dgaTree.GetCachedMethod("performToolAction", ReflectionCache.FlagTypes.InstanceFlags),
                    transpiler: new HarmonyMethod(typeof(FruitTreesAvoidHoe), nameof(FruitTreesAvoidHoe.Transpiler)));
            }
            else
            {
                ModEntry.ModMonitor.Log("Cannot find dga fruit trees; they will still be affected by hoes.", LogLevel.Info);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("transpiling DGA to make fruit trees avoid hoes.", ex);
        }
    }

    [HarmonyPatch(nameof(FruitTree.performToolAction))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Brtrue_S),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Isinst, typeof(Hoe)),
                new(OpCodes.Brtrue_S),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Isinst, typeof(MeleeWeapon)),
            })
            .Remove(6)
            .FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Brtrue_S),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Callvirt, typeof(Tool).GetCachedProperty(nameof(Tool.BaseName), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
                new(OpCodes.Ldstr, "Hoe"),
                new(OpCodes.Callvirt, typeof(string).GetCachedMethod<string>(nameof(string.Contains), ReflectionCache.FlagTypes.InstanceFlags)),
                new(OpCodes.Brfalse_S),
            })
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