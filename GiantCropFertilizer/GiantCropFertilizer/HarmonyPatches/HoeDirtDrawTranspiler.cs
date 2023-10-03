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

namespace GiantCropFertilizer.HarmonyPatches;

/// <summary>
/// Holds transpiler to draw the fertilizer.
/// </summary>
internal static class HoeDirtDrawTranspiler
{
    /// <summary>
    /// Applies patches to draw this fertilizer slightly different.
    /// </summary>
    /// <param name="harmony">Harmony instance.</param>
    /// <remarks>Should avoid this one firing if Multifertilizers is installed.</remarks>
    internal static void ApplyPatches(Harmony harmony)
    {
        harmony.Patch(
            original: typeof(HoeDirt).GetCachedMethod(nameof(HoeDirt.DrawOptimized), ReflectionCache.FlagTypes.InstanceFlags),
            transpiler: new HarmonyMethod(typeof(HoeDirtDrawTranspiler).StaticMethodNamed(nameof(HoeDirtDrawTranspiler.Transpiler))));
    }

    /// <summary>
    /// Gets the correct color for the fertilizer.
    /// </summary>
    /// <param name="prev">The previous fertilizer tint.</param>
    /// <param name="dirt">hoedirt</param>
    /// <returns>A color.</returns>
    [MethodImpl(TKConstants.Hot)]
    private static Color ReplaceColor(Color prev, HoeDirt? dirt)
        => ModEntry.IsGiantCropFertilizer(dirt?.fertilizer.Value) ? Color.Purple : prev;

    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                OpCodes.Ldarg_0,
                (OpCodes.Callvirt, typeof(HoeDirt).GetCachedMethod(nameof(HoeDirt.GetFertilizerSourceRect), ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                 new(OpCodes.Call, typeof(Color).GetCachedProperty(nameof(Color.White), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod()),
            })
            .Advance(1)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(HoeDirtDrawTranspiler).GetCachedMethod(nameof(ReplaceColor), ReflectionCache.FlagTypes.StaticFlags)),
            });

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}