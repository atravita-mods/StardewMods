﻿using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using StardewValley.SpecialOrders;

namespace SpecialOrdersExtended.HarmonyPatches;

[HarmonyPatch(typeof(SpecialOrder))]
internal static class QiQuestTranspiler
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsOptionEnabled() => ModEntry.Config.AvoidRepeatingQiOrders;

    [HarmonyPatch(nameof(SpecialOrder.UpdateAvailableSpecialOrders))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(SpecialCodeInstructionCases.LdLoc),
                new(OpCodes.Ldstr, "Qi"),
                new(OpCodes.Call, typeof(string).GetCachedMethod<string, string>("op_Inequality", ReflectionCache.FlagTypes.StaticFlags)),
            })
            .Advance(3)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Call, typeof(QiQuestTranspiler).GetCachedMethod(nameof(IsOptionEnabled), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Or),
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
