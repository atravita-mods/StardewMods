﻿using System.Reflection;
using System.Reflection.Emit;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using Netcode;
using StardewValley.Menus;

namespace StopRugRemoval.HarmonyPatches.Niceties.CrashHandling;

/// <summary>
/// Holds patches to make special orders less fragile.
/// </summary>
[HarmonyPatch(typeof(SpecialOrder))]
internal static class SpecialOrderCrash
{
    [HarmonyPatch(nameof(SpecialOrder.GetSpecialOrder))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention")]
    private static Exception? Finalizer(string key, ref SpecialOrder? __result, Exception? __exception)
    {
        if (__exception is not null)
        {
            ModEntry.ModMonitor.Log($"Detected invalid special order {key}\n\n{__exception}", LogLevel.Error);
            __result = null;
        }
        return null;
    }

#pragma warning disable SA1116 // Split parameters should start on line after declaration. Reviewed.
    [HarmonyPatch(nameof(SpecialOrder.UpdateAvailableSpecialOrders))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            helper.DeclareLocal(typeof(int), out LocalBuilder? loc)
            .FindLast(new CodeInstructionWrapper[]
            { // r.Next(typed_keys.Count);
                new(SpecialCodeInstructionCases.LdLoc),
                new(SpecialCodeInstructionCases.LdLoc),
                new(OpCodes.Callvirt, typeof(List<string>).GetCachedProperty(nameof(List<string>.Count), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
                new(OpCodes.Callvirt, typeof(Random).GetCachedMethod(nameof(Random.Next), ReflectionCache.FlagTypes.InstanceFlags, new[] { typeof(int) } )),
                new(SpecialCodeInstructionCases.StLoc),
            })
            .GetLabels(out IList<Label>? labelsToMove, clear: true)
            .DefineAndAttachLabel(out Label jumpback)
            .Insert(new CodeInstruction[]
            { // Insert our local so we can't loop forever.
                new(OpCodes.Ldc_I4_5),
                new(OpCodes.Stloc, loc),
            }, withLabels: labelsToMove)
            .FindNext(new CodeInstructionWrapper[]
            { // Find spot to jump back from.
                new(OpCodes.Call, typeof(SpecialOrder).GetCachedMethod(nameof(SpecialOrder.GetSpecialOrder), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Callvirt, typeof(NetList<SpecialOrder, NetRef<SpecialOrder>>).GetCachedMethod("Add", ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .Advance(1)
            .DefineAndAttachLabel(out Label notnull)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Dup), // if we're not null, we're fine.
                new(OpCodes.Brtrue_S, notnull),
                new(OpCodes.Ldloc, loc), // grab our countdown var and decrement it.
                new(OpCodes.Ldc_I4_M1),
                new(OpCodes.Add),
                new(OpCodes.Stloc, loc),
                new(OpCodes.Ldloc, loc),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ble, notnull), // I've tried five times already. Give up.
                new(OpCodes.Pop), // clear the stack before jumping back to try again.
                new(OpCodes.Pop),
                new(OpCodes.Br, jumpback),
            });

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into error transpiling special order board update code to avoid a crash.\n\n{ex}", LogLevel.Error);
            original?.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
#pragma warning restore SA1116 // Split parameters should start on line after declaration
}

/// <summary>
/// Transpiles the special order board to avoid a crash possible if the orders
/// were changed between when they were originally loaded
/// and now.
/// </summary>
[HarmonyPatch(typeof(SpecialOrdersBoard))]
internal static class SpecialOrderBoardCrash
{
    private static void ShowWarning(SpecialOrder order)
        => Game1.showRedMessage(I18n.InvalidSpecialOrder(order.questKey.Value));

    [HarmonyPatch(nameof(SpecialOrdersBoard.receiveLeftClick))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(SpecialOrdersBoard).GetCachedField(nameof(SpecialOrdersBoard.leftOrder), ReflectionCache.FlagTypes.InstanceFlags)),
                new(SpecialCodeInstructionCases.StLoc),
            })
            .Advance(2);

            CodeInstruction? leftorder = helper.CurrentInstruction.ToLdLoc();

            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Call, typeof(SpecialOrder).GetCachedMethod(nameof(SpecialOrder.GetSpecialOrder), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Callvirt, typeof(NetList<SpecialOrder, NetRef<SpecialOrder>>).GetCachedMethod("Add", ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .Advance(1)
            .DefineAndAttachLabel(out Label leftJumpPast)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Dup),
                new(OpCodes.Brtrue_S, leftJumpPast),
                leftorder,
                new(OpCodes.Call, typeof(SpecialOrderBoardCrash).GetCachedMethod(nameof(ShowWarning), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Pop),
                new(OpCodes.Pop),
                new(OpCodes.Ret),
            });

            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(SpecialOrdersBoard).GetCachedField(nameof(SpecialOrdersBoard.rightOrder), ReflectionCache.FlagTypes.InstanceFlags)),
                new(SpecialCodeInstructionCases.StLoc),
            })
            .Advance(2);

            CodeInstruction? rightorder = helper.CurrentInstruction.ToLdLoc();

            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Call, typeof(SpecialOrder).GetCachedMethod(nameof(SpecialOrder.GetSpecialOrder), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Callvirt, typeof(NetList<SpecialOrder, NetRef<SpecialOrder>>).GetCachedMethod("Add", ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .Advance(1)
            .DefineAndAttachLabel(out Label rightJumpPast)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Dup),
                new(OpCodes.Brtrue_S, rightJumpPast),
                rightorder,
                new(OpCodes.Call, typeof(SpecialOrderBoardCrash).GetCachedMethod(nameof(ShowWarning), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Pop),
                new(OpCodes.Pop),
                new(OpCodes.Ret),
            });

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into error transpiling special order board to avoid crash.\n\n{ex}", LogLevel.Error);
            original?.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}