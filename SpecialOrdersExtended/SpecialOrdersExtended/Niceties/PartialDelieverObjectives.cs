using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

namespace SpecialOrdersExtended.Niceties;

[HarmonyPatch(typeof(DeliverObjective))]
internal static class PartialDelieverObjectives
{
    [HarmonyPatch(nameof(DeliverObjective.ShouldShowProgress))]
    private static void Postfix(DeliverObjective __instance, ref bool __result)
    {
        __result = true;
    }

    [HarmonyPatch(nameof(DeliverObjective.OnItemDelivered))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            helper.FindNext(new CodeInstructionWrapper[]
            { // this.GetMaxCount() - this.GetCount();
                new(OpCodes.Ldarg_0),
                new (OpCodes.Call, typeof(OrderObjective).GetCachedMethod(nameof(OrderObjective.GetMaxCount), ReflectionCache.FlagTypes.InstanceFlags)),
                new(OpCodes.Ldarg_0),
                new (OpCodes.Call, typeof(OrderObjective).GetCachedMethod(nameof(OrderObjective.GetCount), ReflectionCache.FlagTypes.InstanceFlags)),
                new (OpCodes.Sub),
            })
            .Copy(5, out var copy)
            .FindNext(new CodeInstructionWrapper[]
            { // Math.Min(item.Stack, this.GetMaxCount() - this.GetCount()
                new(OpCodes.Call, typeof(Math).GetCachedMethod(nameof(Math.Min), ReflectionCache.FlagTypes.StaticFlags, new[] { typeof(int), typeof(int) } )),
            })
            .FindNext(new CodeInstructionWrapper[]
            { // if (required_count > stack) return 0;
                new(SpecialCodeInstructionCases.LdLoc),
                new (SpecialCodeInstructionCases.LdLoc),
                new (OpCodes.Bge_S),
                new (OpCodes.Ldc_I4_0),
                new (OpCodes.Ret),
            })
            .GetLabels(out var labels, clear: true)
            .Remove(5)
            .AttachLabels(labels)
            .FindNext(new CodeInstructionWrapper[]
            { // if (!string.IsNullOrEmpty(this.message.Value)
                new (OpCodes.Ldarg_0),
                new (OpCodes.Ldfld, typeof(DeliverObjective).GetCachedField(nameof(DeliverObjective.message), ReflectionCache.FlagTypes.InstanceFlags)),
                new (OpCodes.Callvirt),
                new (OpCodes.Call, typeof(string).GetCachedMethod(nameof(string.IsNullOrEmpty), ReflectionCache.FlagTypes.InstanceFlags)),
                new (OpCodes.Brtrue_S),
            })
            .Push()
            .Advance(4)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .DefineAndAttachLabel(out var jumppoint)
            .Pop()
            .GetLabels(out var secondLabels, clear: true)
            .Insert(copy.ToArray(), withLabels: secondLabels)
            .Insert(new CodeInstruction[] {new(OpCodes.Brtrue, jumppoint)});

            helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into errors transpiling {original.FullDescription()}.\n\n{ex}", LogLevel.Error);
            original.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}