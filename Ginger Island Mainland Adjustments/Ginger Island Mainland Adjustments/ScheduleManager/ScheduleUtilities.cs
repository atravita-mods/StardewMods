using System.Text;

using AtraShared.Utils.Extensions;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Utilities;

using StardewValley.Pathfinding;

namespace GingerIslandMainlandAdjustments.ScheduleManager;

/// <summary>
/// Class that helps select the right GI remainder schedule.
/// </summary>
internal static class ScheduleUtilities
{
    private const string BASE_SCHEDULE_KEY = "GIRemainder";
    private const string POST_GI_START_TIME = "1800"; // all GI schedules must start at 1800

    private static readonly Dictionary<string, (string key, Dictionary<int, SchedulePathDescription> schedule)> Schedules = new();

    /// <summary>
    /// Removes schedule cache.
    /// </summary>
    internal static void ClearCache() => Schedules.Clear();

    /// <summary>
    /// Appends the correct GI remainder schedule to a schedule being generated.
    /// </summary>
    /// <param name="sb">Stringbuilder that contains the schedule.</param>
    /// <param name="visitor">the visitor.</param>
    /// <returns>same sb instance.</returns>
    internal static StringBuilder AppendCorrectRemainderSchedule(this StringBuilder sb, NPC visitor, out string? key)
    {
        key = null;
        if (visitor.Name.Equals("Gus", StringComparison.OrdinalIgnoreCase))
        {
            // Gus needs to tend bar. Hardcoded same as vanilla.
            sb.Append("1800 Saloon 10 18 2/2430 bed");
        }
        else if (ScheduleUtilities.FindProperGISchedule(visitor, SDate.Now(), out key) is string giSchedule)
        {
            sb.Append(giSchedule);
        }
        else
        {
            sb.Append(Globals.IsChildToNPC?.Invoke(visitor) == true ? "1800 BusStop -1 23 3" : "1800 bed");
        }
        return sb;
    }

    /// <summary>
    /// Find the correct schedule for an NPC for a given date. Looks into the schedule assets first
    /// then sees if there's a GOTO statement. Resolve that if necessary.
    /// </summary>
    /// <param name="npc">NPC to look for.</param>
    /// <param name="date">Date to search.</param>
    /// <param name="key">The schedule key used.</param>
    /// <returns>A schedule string if it can, null if it can't find one.</returns>
    internal static string? FindProperGISchedule(NPC npc, SDate date, out string? key)
    {
        string scheduleKey = BASE_SCHEDULE_KEY;
        if (npc.isMarried())
        {
            Globals.ModMonitor.DebugOnlyLog($"{npc.Name} is married, using married GI schedules");
            scheduleKey += "_married";
        }

        // GIRemainder_Season_Day
        key = $"{scheduleKey}_{date.Season}_{date.Day}";
        if (npc.hasMasterScheduleEntry(key)
            && Globals.UtilitySchedulingFunctions.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(key), out string? scheduleEntry)
            && scheduleEntry.StartsWith(POST_GI_START_TIME))
        {
            return scheduleEntry;
        }

        // GIRemainder_intDay_heartlevel
        int hearts = Utility.GetAllPlayerFriendshipLevel(npc) / 250;
        for (int heartLevel = Math.Max((hearts / 2) * 2, 0); heartLevel > 0; heartLevel--)
        {
            key = $"{scheduleKey}_{date.Day}_{heartLevel}";
            if (npc.hasMasterScheduleEntry(key)
                && Globals.UtilitySchedulingFunctions.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(key), out scheduleEntry)
                && scheduleEntry.StartsWith(POST_GI_START_TIME))
            {
                return scheduleEntry;
            }
        }

        // GIRemainder_Day
        key = $"{scheduleKey}_{date.Day}";
        if (npc.hasMasterScheduleEntry(key)
            && Globals.UtilitySchedulingFunctions.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(key), out scheduleEntry)
            && scheduleEntry.StartsWith(POST_GI_START_TIME))
        {
            return scheduleEntry;
        }

        // GIRemainder_rain
        if (Game1.IsRainingHere(npc.currentLocation))
        {
            key = $"{scheduleKey}_rain";
            if (npc.hasMasterScheduleEntry(key)
                && Globals.UtilitySchedulingFunctions.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(key), out scheduleEntry)
                && scheduleEntry.StartsWith(POST_GI_START_TIME))
            {
                return scheduleEntry;
            }
        }

        // GIRemainder_season_DayOfWeekHearts
        for (int heartLevel = Math.Max((hearts / 2) * 2, 0); heartLevel > 0; heartLevel -= 2)
        {
            key = $"{scheduleKey}_{date.Season}_{Game1.shortDayNameFromDayOfSeason(date.Day)}{heartLevel}";
            if (npc.hasMasterScheduleEntry(key)
                && Globals.UtilitySchedulingFunctions.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(key), out scheduleEntry)
                && scheduleEntry.StartsWith(POST_GI_START_TIME))
            {
                return scheduleEntry;
            }
        }

        // GIRemainder_season_DayOfWeek
        key = $"{scheduleKey}_{date.Season}_{Game1.shortDayNameFromDayOfSeason(date.Day)}";
        if (npc.hasMasterScheduleEntry(key)
            && Globals.UtilitySchedulingFunctions.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(key), out scheduleEntry)
            && scheduleEntry.StartsWith(POST_GI_START_TIME))
        {
            return scheduleEntry;
        }

        // GIRemainder_DayOfWeekHearts
        for (int heartLevel = Math.Max((hearts / 2) * 2, 0); heartLevel > 0; heartLevel -= 2)
        {
            key = $"{scheduleKey}_{Game1.shortDayNameFromDayOfSeason(date.Day)}{heartLevel}";
            if (npc.hasMasterScheduleEntry(key)
                && Globals.UtilitySchedulingFunctions.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(key), out scheduleEntry)
                && scheduleEntry.StartsWith(POST_GI_START_TIME))
            {
                return scheduleEntry;
            }
        }

        // GIRemainder_DayOfWeek
        key = $"{scheduleKey}_{Game1.shortDayNameFromDayOfSeason(date.Day)}";
        if (npc.hasMasterScheduleEntry(key)
            && Globals.UtilitySchedulingFunctions.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(key), out scheduleEntry)
            && scheduleEntry.StartsWith(POST_GI_START_TIME))
        {
            return scheduleEntry;
        }

        // GIRemainderHearts
        for (int heartLevel = Math.Max((hearts / 2) * 2, 0); heartLevel > 0; heartLevel -= 2)
        {
            key = $"{scheduleKey}_{heartLevel}";
            if (npc.hasMasterScheduleEntry(key)
                && Globals.UtilitySchedulingFunctions.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(key), out scheduleEntry)
                && scheduleEntry.StartsWith(POST_GI_START_TIME))
            {
                return scheduleEntry;
            }
        }

        // GIREmainder_season
        key = $"{scheduleKey}_{date.Season}";
        if (npc.hasMasterScheduleEntry(key)
            && Globals.UtilitySchedulingFunctions.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(key), out scheduleEntry)
            && scheduleEntry.StartsWith(POST_GI_START_TIME))
        {
            return scheduleEntry;
        }

        // GIREmainder
        if (npc.hasMasterScheduleEntry(scheduleKey)
            && Globals.UtilitySchedulingFunctions.TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(scheduleKey), out scheduleEntry)
            && scheduleEntry.StartsWith(POST_GI_START_TIME))
        {
            return scheduleEntry;
        }

        Globals.ModMonitor.Log(I18n.NOGISCHEDULEFOUND(npc: npc.Name));
        key = null;
        return null;
    }

    /// <summary>
    /// Wraps npc.parseMasterSchedule to lie to it about the start location of the NPC, if the NPC lives in the farmhouse.
    /// </summary>
    /// <param name="npc">NPC in question.</param>
    /// <param name="key">The schedule key.</param>
    /// <param name="rawData">Raw schedule string.</param>
    /// <returns>True if successful, false otherwise.</returns>
    internal static bool ParseMasterScheduleAdjustedForChild2NPC(NPC npc, string key, string rawData)
    {
        if (Globals.IsChildToNPC?.Invoke(npc) == true)
        {
            // For a Child2NPC, we must handle their scheduling ourselves.
            if (Globals.UtilitySchedulingFunctions.TryFindGOTOschedule(npc, SDate.Now(), rawData, out string scheduleString))
            {
                Dictionary<int, SchedulePathDescription>? schedule = Globals.UtilitySchedulingFunctions.ParseSchedule(key, scheduleString, npc, "BusStop", new Point(-1, 23), 610, Globals.Config.EnforceGITiming);
                if (schedule is not null)
                {
                    npc.TryLoadSchedule(key, schedule);
                    return true;
                }
                else
                {
                    Globals.ModMonitor.Log($"Failed to generate schedule for {npc.Name}: {rawData}");
                    return false;
                }
            }
            else
            {
                Globals.ModMonitor.Log("TryFindGOTOschedule failed for Child2NPC!", LogLevel.Warn);
                return false;
            }
        }
        else if ((npc.DefaultMap.Equals("FarmHouse", StringComparison.Ordinal) || npc.DefaultMap.Contains("Cabin", StringComparison.Ordinal))
                  && !npc.isMarried())
        {
            // lie to parse master schedule
            string prevmap = npc.DefaultMap;
            Vector2 prevposition = npc.DefaultPosition;

            if (rawData.EndsWith("bed"))
            {
                rawData = rawData[..^3] + "BusStop -1 23 3";
            }

            npc.DefaultMap = "BusStop";
            npc.DefaultPosition = new Vector2(0, 23) * 64;
            Dictionary<int, SchedulePathDescription>? schedule = null;
            try
            {
                schedule = npc.parseMasterSchedule(key, rawData);
            }
            catch (Exception ex)
            {
                Globals.ModMonitor.LogError($"parsing schedule '{rawData}' for '{npc.Name}'", ex);
            }
            npc.DefaultMap = prevmap;
            npc.DefaultPosition = prevposition;

            if (schedule is not null)
            {
                npc.TryLoadSchedule(key, schedule);
                Schedules[npc.Name] = (key, new(schedule));
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            Dictionary<int, SchedulePathDescription>? schedule = null;

            if (!rawData.StartsWith("0 ") && npc.DefaultPosition != Vector2.Zero && (npc.currentLocation.Name != npc.DefaultMap || npc.DefaultPosition != npc.Position))
            {
                Globals.ModMonitor.Log($"Warping {npc.Name} back to their default location....");
                GameLocation? location = Game1.getLocationFromName(npc.DefaultMap);
                if (location is null)
                {
                    Globals.ModMonitor.Log($"NPC {npc.Name} has default map {npc.DefaultMap} which could not be found!", LogLevel.Warn);
                    return false;
                }
                Game1.warpCharacter(npc, location, npc.DefaultPosition / 64f);
            }

            try
            {
                schedule = npc.parseMasterSchedule(key, rawData);
            }
            catch (Exception ex)
            {
                Globals.ModMonitor.LogError($"parsing schedule for npc '{npc.Name}' with rawdata '{rawData}'", ex);
            }
            if (schedule is not null)
            {
                npc.TryLoadSchedule(key, schedule);
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Goes around at about 620 and fixes up schedules if they've been nulled.
    /// Child2NPC may try to fix up their children around ~610 AM. Sadly, that nulls the schedules.
    /// </summary>
    internal static void FixUpSchedules()
    {
        foreach (NPC npc in Game1.getLocationFromName("FarmHouse").characters)
        {
            if (npc.Schedule is null && Globals.IsChildToNPC?.Invoke(npc) == true
                && ScheduleUtilities.Schedules.TryGetValue(npc.Name, out (string key, Dictionary<int, SchedulePathDescription> schedule) pair))
            {
                Globals.ModMonitor.Log($"Fixing up schedule for {npc.Name}, which appears to have been nulled.", LogLevel.Warn);
                npc.TryLoadSchedule(pair.key, pair.schedule);
                ScheduleUtilities.Schedules.Remove(npc.Name);
            }
        }
    }
}