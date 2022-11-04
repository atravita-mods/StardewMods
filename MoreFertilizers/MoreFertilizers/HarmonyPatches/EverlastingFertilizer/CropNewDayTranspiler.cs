using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;
using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.EverlastingFertilizer;

[HarmonyPatch(typeof(Crop))]
internal static class CropNewDayTranspiler
{
    /// <summary>
    /// Applies patch against DGA's crop.newday.
    /// </summary>
    /// <param name="harmony">harmony instance.</param>
    internal static void ApplyDGAPatches(Harmony harmony)
    {
        try
        {
            Type dgaCrop = AccessTools.TypeByName("DynamicGameAssets.Game.CustomCrop")
                ?? ReflectionThrowHelper.ThrowMethodNotFoundException<Type>("DGA Crop");

            harmony.Patch(
                original: dgaCrop.GetCachedMethod("NewDay", ReflectionCache.FlagTypes.InstanceFlags),
                transpiler: new(typeof(CropNewDayTranspiler).GetCachedMethod(nameof(Transpiler), ReflectionCache.FlagTypes.StaticFlags)));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling DGA. Integration may not work correctly.\n\n{ex}", LogLevel.Error);
        }
    }

    [HarmonyPatch(nameof(Crop.newDay))]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = "Reviewed.")]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(SpecialCodeInstructionCases.LdArg, 5),
                new(OpCodes.Ldfld, typeof(GameLocation).GetCachedField(nameof(GameLocation.isOutdoors), ReflectionCache.FlagTypes.InstanceFlags)),
                new(OpCodes.Call), // this is an op_implicit
                new(OpCodes.Brfalse_S),
            })
            .Push()
            .Advance(3)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .DefineAndAttachLabel(out var jumppoint)
            .Pop()
            .GetLabels(out var labels)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_2), // fertilizer
                new(OpCodes.Call, typeof(ModEntry).GetCachedProperty(nameof(ModEntry.EverlastingFertilizerID), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod(true)),
                new(OpCodes.Beq, jumppoint),
            }, withLabels: labels);

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling {original.FullDescription()}:\n\n{ex}", LogLevel.Error);
            original.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}