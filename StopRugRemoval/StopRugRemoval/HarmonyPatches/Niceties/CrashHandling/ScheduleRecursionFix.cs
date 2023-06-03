using System.Reflection;
using System.Reflection.Emit;

using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

namespace StopRugRemoval.HarmonyPatches.Niceties.CrashHandling;

#warning - crosscheck in 1.6.

/// <summary>
/// Prevents default->default and spring->spring recursion in scheduling.
/// </summary>
[HarmonyPatch(typeof(NPC))]
internal static class ScheduleRecursionFix
{
    private static readonly Lazy<Func<NPC, string>> LastLoadedScheduleGetter = new(
        () => typeof(NPC).GetCachedField("_lastLoadedScheduleKey", ReflectionCache.FlagTypes.InstanceFlags)
                         .GetInstanceFieldGetter<NPC, string>());

    private static bool CheckForRecursion(string schedule,  NPC npc)
    {
        if (LastLoadedScheduleGetter.Value(npc) == schedule)
        {
            ModEntry.ModMonitor.Log($"Schedule disabled for {npc.Name} - infinite recursion found.", LogLevel.Error);
            return true;
        }
        return false;
    }

    [HarmonyPatch(nameof(NPC.parseMasterSchedule))]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = "Reviewed.")]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            { // if (this.changeScheduleForLocationAccessibility)
                (OpCodes.Call, typeof(NPC).GetCachedMethod("changeScheduleForLocationAccessibility", ReflectionCache.FlagTypes.InstanceFlags)),
            });

            foreach (string? schedulestring in new[] { "default", "spring" })
            {
                helper.FindNext(new CodeInstructionWrapper[]
                { // return this.parseMasterSchedule(this.GetMasterScheduleEntry("default" OR "spring");
                    OpCodes.Ldarg_0,
                    OpCodes.Ldarg_0,
                    (OpCodes.Ldstr, schedulestring),
                    (OpCodes.Call, typeof(NPC).GetCachedMethod(nameof(NPC.getMasterScheduleEntry), ReflectionCache.FlagTypes.InstanceFlags)),
                    (OpCodes.Call, typeof(NPC).GetCachedMethod(nameof(NPC.parseMasterSchedule), ReflectionCache.FlagTypes.InstanceFlags)),
                })
                .GetLabels(out IList<Label>? defaultLabels)
                .DefineAndAttachLabel(out Label defaultJump)
                .Insert(new CodeInstruction[]
                {
                    new(OpCodes.Ldstr, schedulestring),
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Call, typeof(ScheduleRecursionFix).GetCachedMethod(nameof(CheckForRecursion), ReflectionCache.FlagTypes.StaticFlags)),
                    new(OpCodes.Brfalse, defaultJump),
                    new(OpCodes.Ldnull),
                    new(OpCodes.Ret),
                }, withLabels: defaultLabels);
            }

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
