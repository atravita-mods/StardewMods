using System.Text;

using AtraBase.Toolkit.Extensions;

using AtraShared.Schedules.DataModels;
using AtraShared.Utils.Extensions;
using GingerIslandMainlandAdjustments.CustomConsoleCommands;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Utilities;

namespace GingerIslandMainlandAdjustments.ScheduleManager.DataModels;

/// <summary>
/// Handles scheduling some islanders in IslandNorth.
/// </summary>
internal static class IslandNorthScheduler
{
    #region points

    /// <summary>
    /// IslandNorth points for the adventurous.
    /// </summary>
    private static readonly Point[] CloseAdventurousPoint = new[]
    {
        new Point(33, 83),
        new Point(36, 81),
        new Point(39, 83),
    };

    private static readonly Point[] TentAdventurousPoint = new[]
    {
        new Point(44, 51),
        new Point(47, 49),
        new Point(50, 51),
    };

    private static readonly Point[] VolcanoAdventurousPoint = new[]
    {
        new Point(46, 29),
        new Point(48, 26),
        new Point(51, 28),
    };

    #endregion

    /// <summary>
    /// Makes schedules for the.
    /// </summary>
    /// <param name="random">Seeded random.</param>
    /// <param name="explorers">Hashset of explorers.</param>
    /// <param name="explorergroup">The name of the explorer group.</param>
    internal static void Schedule(Random random, HashSet<NPC> explorers, string explorergroup)
    {
        if (explorers.Count > 0)
        {
            bool whichFarpoint = random.OfChance(0.5);
            Point[] farPoints = whichFarpoint ? TentAdventurousPoint : VolcanoAdventurousPoint;
            string whichDialogue = whichFarpoint ? "Tent" : "Volcano";
            NPC[] explorerList = explorers.ToArray();
            Dictionary<NPC, StringBuilder> schedules = [];
            int explorerIndex = 0;

            foreach (NPC explorer in explorers)
            {
                SchedulePoint firstPoint = new(
                    random: random,
                    npc: explorer,
                    map: "IslandNorth",
                    time: 1200,
                    point: CloseAdventurousPoint[explorerIndex++],
                    isarrivaltime: true,
                    basekey: "Resort_Adventure",
                    varKey: $"Resort_Adventure_{explorergroup}",
                    direction: explorerIndex); // this little hackish thing makes them face in different directions.
                schedules[explorer] = firstPoint.AppendToStringBuilder(new());
            }

            explorerIndex = 0;
            Utility.Shuffle(random, explorerList);
            foreach (NPC explorer in explorerList)
            {
                schedules[explorer].Append('/');
                new SchedulePoint(
                    random: random,
                    npc: explorer,
                    map: "IslandNorth",
                    time: 1330,
                    point: farPoints[explorerIndex++],
                    basekey: $"Resort_{whichDialogue}",
                    varKey: $"Resort_{whichDialogue}_{explorergroup}",
                    direction: explorerIndex).AppendToStringBuilder(schedules[explorer]);
            }

            explorerIndex = 0;
            Utility.Shuffle(random, explorerList);
            foreach (NPC explorer in explorerList)
            {
                StringBuilder sb = schedules[explorer];
                sb.Append('/');
                new SchedulePoint(
                    random: random,
                    npc: explorer,
                    map: "IslandNorth",
                    time: 1700,
                    point: CloseAdventurousPoint[explorerIndex++],
                    basekey: "Resort_AdventureReturn",
                    varKey: $"Resort_AdventureReturn_{explorergroup}",
                    isarrivaltime: true,
                    direction: explorerIndex).AppendToStringBuilder(sb);

                sb.Append('/')
                  .AppendCorrectRemainderSchedule(explorer, out _);

                string renderedSchedule = sb.ToString();
                sb.Clear();

                Globals.ModMonitor.DebugOnlyLog($"Calculated island north schedule for {explorer.Name}: {renderedSchedule}");
                explorer.islandScheduleName.Value = "island";

                if (ScheduleUtilities.ParseMasterScheduleAdjustedForChild2NPC(explorer, "island", renderedSchedule))
                {
                    Game1.netWorldState.Value.IslandVisitors.Add(explorer.Name);
                    ConsoleCommands.IslandSchedules[explorer.Name] = renderedSchedule;
                }
            }
        }
    }
}