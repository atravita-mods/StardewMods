namespace StopRugRemoval.HarmonyPatches.Niceties.CrashHandling;

using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Pathfinding;

/// <summary>
/// Prevents default->default and spring->spring recursion in scheduling.
/// </summary>
[HarmonyPatch(typeof(NPC))]
internal static class ScheduleRecursionFix
{
    private static readonly ThreadLocal<Stack<string>> _stack = new(() => new());

    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(nameof(NPC.parseMasterSchedule))]
    private static bool Prefix(NPC __instance, string scheduleKey, string rawData, ref Dictionary<int, SchedulePathDescription>? __result)
    {
        _stack.Value ??= new();

        ModEntry.ModMonitor.DebugOnlyLog($"Pushing {scheduleKey} for {__instance.Name}");

        if (_stack.Value.Contains(scheduleKey))
        {
            ModEntry.ModMonitor.Log($"Broke schedule loop for NPC {__instance.Name}: {string.Join("->", _stack.Value)}.\n\tRaw data was {rawData}", LogLevel.Warn);
            _stack.Value.Push(scheduleKey);
            __result = [];
            return false;
        }

        _stack.Value.Push(scheduleKey);
        return true;
    }

    [HarmonyPatch(nameof(NPC.parseMasterSchedule))]
    private static void Finalizer(NPC __instance, string scheduleKey, string rawData, Exception __exception)
    {
        if (__exception is not null)
        {
            ModEntry.ModMonitor.Log($"Schedule parsing ran into errors for npc '{__instance.Name}' with key '{scheduleKey}': {rawData}.", LogLevel.Warn);
            ModEntry.ModMonitor.Log(__exception.ToString());
            _stack.Value?.Clear();
            return;
        }

        if (_stack.Value is not { } s || !s.TryPop(out string? last))
        {
            ModEntry.ModMonitor.Log($"Huh, how did we get here? {Environment.CurrentManagedThreadId}.", LogLevel.Warn);
            return;
        }
        else
        {
            ModEntry.ModMonitor.DebugOnlyLog($"Leaving processing {last} for {__instance.Name}");
        }
    }
}
