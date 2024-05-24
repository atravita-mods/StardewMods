﻿using System.Reflection;
using System.Reflection.Emit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

namespace SleepInWedding.HarmonyPatches;

/// <summary>
/// Removes the multiplayer sync at the end of the wedding.
/// </summary>
[HarmonyPatch]
internal static class RemoveEndWeddingCheck
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return typeof(Utility).GetCachedMethod(nameof(Utility.getWeddingEvent), ReflectionCache.FlagTypes.StaticFlags);
        yield return typeof(Utility).GetCachedMethod(nameof(Utility.getPlayerWeddingEvent), ReflectionCache.FlagTypes.StaticFlags);
    }

    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = StyleCopConstants.SplitParametersIntentional)]
    [SuppressMessage("SMAPI.CommonErrors", "AvoidNetField:Avoid Netcode types when possible", Justification = StyleCopConstants.UsedForMatchingOnly)]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1122:Use string.Empty for empty strings", Justification = "Intentionally using string literal.")]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindLast(new CodeInstructionWrapper[]
            {
                (OpCodes.Ldstr, "\"/pause 4000/waitForOtherPlayers weddingEnd"),
            })
            .ReplaceOperand("\"/pause 4000")
            .FindNext(new CodeInstructionWrapper[]
            {
                OpCodes.Ldarg_0,
                (OpCodes.Ldfld, typeof(Farmer).GetCachedField(nameof(Farmer.uniqueMultiplayerID), ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .GetLabels(out IList<Label>? labelsToMove)
            .RemoveUntil(new CodeInstructionWrapper[]
            {
                OpCodes.Stelem_Ref,
                OpCodes.Dup,
                (OpCodes.Ldc_I4_S, 24),
            })
            .Insert(new CodeInstruction[]
            {
                new (OpCodes.Ldstr, ""),
            }, withLabels: labelsToMove);

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
