using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Niceties;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

namespace StopRugRemoval.HarmonyPatches.Niceties.CrashHandling;

#warning - finish this?

[HarmonyPatch(typeof(NPC))]
internal static class ScheduleRecursionFix
{
    private static void AlertPlayer(NPC npc)
    {
        ModEntry.ModMonitor.Log($"Schedule disabled for {npc.Name} - recursion found.", LogLevel.Error);
    }

    [HarmonyPatch(nameof(NPC.parseMasterSchedule))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            { // if (this.changeScheduleForLocationAccessibility)
                (OpCodes.Call, typeof(NPC).GetCachedMethod("changeScheduleForLocationAccessibility", ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                (OpCodes.Ldstr, "default"),
                (OpCodes.Call, typeof(NPC).GetCachedMethod(nameof(NPC.getMasterScheduleEntry), ReflectionCache.FlagTypes.InstanceFlags)),
                (OpCodes.Call, typeof(NPC).GetCachedMethod(nameof(NPC.parseMasterSchedule), ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .Advance(1);

            helper.Print();
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
