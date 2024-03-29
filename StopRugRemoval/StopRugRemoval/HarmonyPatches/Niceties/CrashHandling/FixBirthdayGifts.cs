﻿using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.StringHandler;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using Netcode;

namespace StopRugRemoval.HarmonyPatches.Niceties.CrashHandling;

/// <summary>
/// Holds patches that fixes the birthday gift crash.
/// </summary>
[HarmonyPatch]
internal static class FixBirthdayGifts
{
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(NPC), nameof(NPC.getFavoriteItem))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
    private static Exception? FinalizeGiftSelection(Exception __exception, ref SObject? __result, NPC __instance)
    {
        if (__exception is not null)
        {
            ModEntry.ModMonitor.Log($"{__instance.Name}'s birthday gift seems invalid, original exception {__exception}", LogLevel.Trace);
            if (Game1.NPCGiftTastes.TryGetValue(__instance.Name, out string? likes))
            {
                ReadOnlySpan<char> loves = likes.GetNthChunk('/', NPC.gift_taste_love + 1);
                foreach (SpanSplitEntry seg in loves.StreamSplit(options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (int.TryParse(seg, out int val) && val > 0)
                    {
                        __result = new SObject(val, 1);
                        return null;
                    }
                }
            }

            ModEntry.ModMonitor.Log($"Failed to find replacement gift for {__instance.Name}, surpressing original exception.", LogLevel.Error);
            __result = null;
        }
        return null;
    }

    [HarmonyPatch(typeof(SObject), nameof(SObject.DayUpdate))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            Label label = helper.Generator.DefineLabel();
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Callvirt, typeof(NPC).GetCachedMethod(nameof(NPC.getFavoriteItem), ReflectionCache.FlagTypes.InstanceFlags)),
                new(OpCodes.Callvirt, typeof(NetFieldBase<SObject, NetRef<SObject>>).GetCachedProperty("Value", ReflectionCache.FlagTypes.InstanceFlags).GetSetMethod()),
            })
            .Advance(1)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Dup), // duplicate so I have one to test against.
                new(OpCodes.Brfalse_S, label),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Br),
            })
            .Advance(1)
            .Insert(new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Pop).WithLabels(label),
                new CodeInstruction(OpCodes.Pop),
            });

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into error transpiling {original.FullDescription()}.\n\n{ex}", LogLevel.Error);
            original?.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}