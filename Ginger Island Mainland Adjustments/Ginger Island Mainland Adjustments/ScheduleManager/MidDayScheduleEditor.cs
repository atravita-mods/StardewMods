using AtraBase.Utils.Extensions;
using AtraShared.Utils.Extensions;
using GingerIslandMainlandAdjustments.Utils;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace GingerIslandMainlandAdjustments.ScheduleManager;

/// <summary>
/// Class that handles middle of the day schedule editing.
/// </summary>
internal static class MidDayScheduleEditor
{
    /// <summary>
    /// When Ginger Island schedules end.
    /// </summary>
    private const int GIEndTime = 1800;

    /// <summary>
    /// Which map is the resort on.
    /// </summary>
    private const string GIMap = "IslandSouth";

    /// <summary>
    /// Keep track of the NPCs I've edited already, so I don't edit anyone twice.
    /// </summary>
    private static readonly Dictionary<string, bool> ScheduleAltered = new();

    /// <summary>
    /// Clears the ScheduleAltered dictionary.
    /// </summary>
    public static void Reset()
    {
        ScheduleAltered.Clear();
        Globals.ModMonitor.Log("Reset scheduleAltered", LogLevel.Trace);
    }

    /// <summary>
    /// Attempt a mid-day adjustment of a character's schedule
    /// if they're headed to Ginger Island.
    /// Does one per ten minutes.
    /// </summary>
    /// <param name="e">Time Changed Parameters from SMAPI.</param>
    public static void AttemptAdjustGISchedule(TimeChangedEventArgs e)
    {
        if (e.NewTime >= 900)
        { // skip after 9AM.
            return;
        }
        if (Globals.Config.UseThisScheduler)
        {// fancy scheduler is on.
            return;
        }
        foreach (string name in Game1.netWorldState.Value.IslandVisitors.Keys)
        {
            if (name.Equals("Gus", StringComparison.OrdinalIgnoreCase))
            { // Gus runs saloon, skip.
                continue;
            }
            if (!Game1.netWorldState.Value.IslandVisitors[name])
            {
                continue;
            }
            if (ScheduleAltered.TryGetValue(name, out bool hasbeenaltered) && hasbeenaltered)
            {
                continue;
            }
            Globals.ModMonitor.Log(I18n.MiddayScheduleEditor_NpcFoundForAdjustment(name), LogLevel.Trace);
            ScheduleAltered[name] = true;
            NPC npc = Game1.getCharacterFromName(name);
            if (npc is not null)
            {
                AdjustSpecificSchedule(npc);
                break; // Do the next person at the next ten minute tick.
            }
        }
    }

    /// <summary>
    /// Midday adjustment of a schedule for a specific NPC.
    /// </summary>
    /// <param name="npc">NPC who's schedule may need adjusting.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public static bool AdjustSpecificSchedule(NPC npc)
    {
        if (npc.islandScheduleName?.Value is null || npc.islandScheduleName?.Value == string.Empty)
        {
            if (Globals.Config.EnforceGITiming)
            {
                Globals.ModMonitor.Log(I18n.MiddayScheduleEditor_NpcNotIslander(npc.Name), LogLevel.Warn);
            }
            return false;
        }
        if (npc.IsInvisible)
        {
            Globals.ModMonitor.DebugLog($"NPC {npc.Name} is invisible, not altering schedule", LogLevel.Trace);
            return false;
        }
        if (npc.Schedule is null)
        {
            if (Globals.Config.EnforceGITiming)
            {
                Globals.ModMonitor.Log(I18n.MiddayScheduleEditor_NpcHasNoSchedule(npc.Name), LogLevel.Warn);
            }
            return false;
        }

        List<int> keys = npc.Schedule.Keys.ToList();
        keys.Sort();
        if (keys.Count == 0 || keys[^1] != GIEndTime)
        {
            Globals.ModMonitor.DebugLog($"Recieved {npc.Name} to adjust but last schedule key is not {GIEndTime}");
            return false;
        }
        string? schedule = ScheduleUtilities.FindProperGISchedule(npc, SDate.Now());
        Dictionary<int, SchedulePathDescription>? remainderSchedule = ParseGIRemainderSchedule(schedule, npc);

        if (remainderSchedule is not null)
        {
            npc.Schedule.Update(remainderSchedule);
        }
        return remainderSchedule is null;
    }

    /// <summary>
    /// Gets the SchedulePathDescription schedule for the schedule string,
    /// assumes a 1800 start on Ginger Island.
    /// </summary>
    /// <param name="schedule">Raw schedule string.</param>
    /// <param name="npc">NPC.</param>
    /// <returns>null if the schedule could not be parsed, a schedule otherwise.</returns>
    [ContractAnnotation("schedule:null => null")]
    public static Dictionary<int, SchedulePathDescription>? ParseGIRemainderSchedule(string? schedule, NPC npc)
    {
        if (schedule is null)
        {
            return null;
        }

        Point lastStop = npc.Schedule[GIEndTime].route.Peek();
        int lasttime = GIEndTime - 10;

        return Globals.UtilityFunctions.ParseSchedule(schedule, npc, GIMap, lastStop, lasttime);
    }
}
