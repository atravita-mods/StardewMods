using System.Reflection;
using System.Reflection.Emit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using StardewValley.Monsters;

namespace CritterRings.HarmonyPatches.OwlRing;

/// <summary>
/// A patch so the farmer is seen less by lava lurks.
/// </summary>
[HarmonyPatch(typeof(LavaLurk))]
internal static class LavaLurkNerf
{
    private static float AdjustLavaLurkDistance(float original, Farmer farmer)
        => farmer.isWearingRing(ModEntry.OwlRing) ? original / 2 : original;

    [HarmonyPatch(nameof(LavaLurk.TargetInRange))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                (OpCodes.Call, typeof(Math).GetCachedMethod<float>(nameof(Math.Abs), ReflectionCache.FlagTypes.StaticFlags)),
                (OpCodes.Ldc_R4, 640f),
            })
            .Advance(2)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(LavaLurk).GetCachedField(nameof(LavaLurk.targettedFarmer), ReflectionCache.FlagTypes.InstanceFlags)),
                new(OpCodes.Call, typeof(LavaLurkNerf).GetCachedMethod(nameof(AdjustLavaLurkDistance), ReflectionCache.FlagTypes.StaticFlags)),
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
