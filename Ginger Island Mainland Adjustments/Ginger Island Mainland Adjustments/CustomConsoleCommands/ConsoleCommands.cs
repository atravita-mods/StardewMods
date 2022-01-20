using System.Text;
using GingerIslandMainlandAdjustments.ScheduleManager;
using GingerIslandMainlandAdjustments.Utils;
using StardewModdingAPI.Utilities;

namespace GingerIslandMainlandAdjustments.CustomConsoleCommands;

/// <summary>
/// Class that handles this mod's console commands.
/// </summary>
internal static class ConsoleCommands
{
    /// <summary>
    /// Register the console commands for this mod.
    /// </summary>
    /// <param name="commandHelper">SMAPI's console command helper.</param>
    public static void Register(ICommandHelper commandHelper)
    {
        commandHelper.Add(
            name: "get_schedule",
            documentation: I18n.GetSchedule_Documentation(),
            callback: ConsoleSchedule);
        commandHelper.Add(
            name: "get_islanders",
            documentation: I18n.GetIslanders_Documentation(),
            callback: ConsoleGetIslanders);
    }

    /// <summary>
    /// Displays the schedule of a specific NPC.
    /// </summary>
    /// <param name="npc">NPC in question.</param>
    /// <param name="level">Log level to display.</param>
    private static void DisplaySchedule(NPC npc, LogLevel level)
    {
        Globals.ModMonitor.Log(I18n.DisplaySchedule_CheckNPC(npc.Name), level);
        if (npc.IsInvisible)
        {
            Globals.ModMonitor.Log('\t' + I18n.DisplaySchedule_IsInvisible(npc.Name), level);
            return;
        }
        if (!npc.followSchedule)
        {
            Globals.ModMonitor.Log('\t' + I18n.DisplaySchedule_NoSchedule(npc.Name), level);
            if (npc.Schedule == null || npc.Schedule.Keys.Count == 0)
            { // For some reason, sometimes even when followSchedule is not set, the NPC goes through their schedule anyways?
                return;
            }
        }
        Game1.netWorldState.Value.IslandVisitors.TryGetValue(npc.Name, out bool atIsland);
        if (atIsland)
        {
            Globals.ModMonitor.Log('\t' + I18n.DisplaySchedule_ToIsland(npc.Name), level);
        }
        else
        {
            ScheduleUtilities.TryFindGOTOschedule(npc, SDate.Now(), npc.getMasterScheduleEntry(npc.dayScheduleName.Value), out string schedulestring);
            Globals.ModMonitor.Log($"\t{npc.dayScheduleName.Value}\n\t\t{schedulestring}", level);
        }
        List<int> keys = new(npc.Schedule.Keys);
        keys.Sort();
        StringBuilder sb = new();
        foreach (int key in keys)
        {
            SchedulePathDescription schedulePathDescription = npc.Schedule[key];
            sb.Append('\t').Append(key).Append(": ");
            sb.AppendJoin(", ", schedulePathDescription.route).AppendLine();
            sb.Append("\t\t").Append(I18n.DisplaySchedule_ExpectedTime()).Append(schedulePathDescription.GetExpectedRouteTime()).AppendLine(" minutes");
            sb.Append("\t\t").Append(I18n.DisplaySchedule_Direction()).AppendLine(schedulePathDescription.facingDirection.ToString());
            sb.Append("\t\t").Append(I18n.DisplaySchedule_Animation()).AppendLine(schedulePathDescription.endOfRouteBehavior);
            sb.Append("\t\t").Append(I18n.DisplaySchedule_Message()).AppendLine(schedulePathDescription.endOfRouteMessage);
        }

        Globals.ModMonitor.Log(sb.ToString(), level);
    }

    /// <summary>
    /// Console command to get the islanders.
    /// </summary>
    /// <param name="command">Name of command.</param>
    /// <param name="args">Arguments, if any.</param>
    private static void ConsoleGetIslanders(string command, string[] args) => Globals.ModMonitor.Log($"{I18n.Islanders()}: {string.Join(", ", Islanders.Get())}", LogLevel.Debug);

    /// <summary>
    /// Yields the schedule for one or more characters.
    /// </summary>
    /// <param name="command">Name of command.</param>
    /// <param name="args">List of islanders.</param>
    private static void ConsoleSchedule(string command, string[] args)
    {
        foreach (string name in args)
        {
            NPC? npc = Game1.getCharacterFromName(name, true);
            if (npc is not null)
            {
                DisplaySchedule(npc, LogLevel.Debug);
            }
            else
            {
                Globals.ModMonitor.Log(I18n.NpcNotFound(name), LogLevel.Debug);
            }
        }
    }
}
