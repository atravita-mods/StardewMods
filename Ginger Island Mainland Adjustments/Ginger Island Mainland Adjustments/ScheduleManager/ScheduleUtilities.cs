using System.Text.RegularExpressions;
using AtraShared.Utils.Extensions;
using GingerIslandMainlandAdjustments.Utils;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Utilities;
using StardewValley.Network;

namespace GingerIslandMainlandAdjustments.ScheduleManager;

/// <summary>
/// Holds a warp location.
/// </summary>
public class WarpPoint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WarpPoint"/> class.
    /// </summary>
    /// <param name="mapName">Map name as string.</param>
    /// <param name="location">Vector2 location to warp to.</param>
    public WarpPoint(string mapName, Vector2 location)
    {
        this.MapName = mapName;
        this.Location = location;
    }

    /// <summary>
    /// Gets map name as string.
    /// </summary>
    public string MapName { get; private set; }

    /// <summary>
    /// Gets tile to warp to as Vector2.
    /// </summary>
    public Vector2 Location { get; private set; }
}

/// <summary>
/// Class that helps select the right GI remainder schedule.
/// </summary>
internal static class ScheduleUtilities
{
    private const string BASE_SCHEDULE_KEY = "GIRemainder";
    private const string POST_GI_START_TIME = "1800"; // all GI schedules must start at 1800

    private static readonly Dictionary<string, Dictionary<int, SchedulePathDescription>> Schedules = new();

    /// <summary>
    /// Removes schedule cache.
    /// </summary>
    public static void ClearCache()
    {
        Schedules.Clear();
    }

    /// <summary>
    /// Find the correct schedule for an NPC for a given date. Looks into the schedule assets first
    /// then sees if there's a GOTO statement. Resolve that if necessary.
    /// </summary>
    /// <param name="npc">NPC to look for.</param>
    /// <param name="date">Date to search.</param>
    /// <returns>A schedule string if it can, null if it can't find one.</returns>
    public static string? FindProperGISchedule(NPC npc, SDate date)
    {
        string scheduleKey = BASE_SCHEDULE_KEY;
        if (npc.isMarried())
        {
            Globals.ModMonitor.DebugLog($"{npc.Name} is married, using married GI schedules");
            scheduleKey += "_married";
        }
        int hearts = Utility.GetAllPlayerFriendshipLevel(npc) / 250;

        // GIRemainder_Season_Day
        string checkKey = $"{scheduleKey}_{date.Season}_{date.Day}";
        string? scheduleEntry;
        if (npc.hasMasterScheduleEntry(checkKey)
            && TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(checkKey), out scheduleEntry)
            && scheduleEntry.StartsWith(POST_GI_START_TIME))
        {
            return scheduleEntry;
        }

        // GIRemainder_intDay_heartlevel
        for (int heartLevel = Math.Max((hearts / 2) * 2, 0); heartLevel > 0; heartLevel--)
        {
            checkKey = $"{scheduleKey}_{date.Day}_{heartLevel}";
            if (npc.hasMasterScheduleEntry(checkKey)
                && TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(checkKey), out scheduleEntry)
                && scheduleEntry.StartsWith(POST_GI_START_TIME))
            {
                return scheduleEntry;
            }
        }

        // GIRemainder_Day
        checkKey = $"{scheduleKey}_{Game1.dayOfMonth}";
        if (npc.hasMasterScheduleEntry(checkKey)
            && TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(checkKey), out scheduleEntry)
            && scheduleEntry.StartsWith(POST_GI_START_TIME))
        {
            return scheduleEntry;
        }

        // GIRemainder_rain
        if (Game1.IsRainingHere(npc.currentLocation))
        {
            checkKey = $"{scheduleKey}_rain";
            if (npc.hasMasterScheduleEntry(checkKey)
                && TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(checkKey), out scheduleEntry)
                && scheduleEntry.StartsWith(POST_GI_START_TIME))
            {
                return scheduleEntry;
            }
        }

        // GIRemainderHearts
        for (int heartLevel = Math.Max((hearts / 2) * 2, 0); heartLevel > 0; heartLevel -= 2)
        {
            checkKey = $"{scheduleKey}_{date.Day}_{heartLevel}";
            if (npc.hasMasterScheduleEntry(checkKey)
                && TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(checkKey), out scheduleEntry)
                && scheduleEntry.StartsWith(POST_GI_START_TIME))
            {
                return scheduleEntry;
            }
        }

        // GIRemainder_DayOfWeek
        checkKey = $"{scheduleKey}_{Game1.shortDayNameFromDayOfSeason(date.Day)}";
        if (npc.hasMasterScheduleEntry(checkKey)
            && TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(checkKey), out scheduleEntry)
            && scheduleEntry.StartsWith(POST_GI_START_TIME))
        {
            return scheduleEntry;
        }

        // GIREmainder
        if (npc.hasMasterScheduleEntry(scheduleKey)
            && TryFindGOTOschedule(npc, date, npc.getMasterScheduleEntry(scheduleKey), out scheduleEntry)
            && scheduleEntry.StartsWith(POST_GI_START_TIME))
        {
            return scheduleEntry;
        }

        Globals.ModMonitor.Log(I18n.NOGISCHEDULEFOUND(npc: npc.Name));
        return null;
    }

    /// <summary>
    /// Given a raw schedule string, returns a new raw schedule string, after following the GOTO/MAIL/NOT friendship keys in the game.
    /// </summary>
    /// <param name="npc">NPC.</param>
    /// <param name="date">The data to analyze.</param>
    /// <param name="rawData">The raw schedule string.</param>
    /// <param name="scheduleString">A raw schedule string, stripped of MAIL/GOTO/NOT elements. Ready to be parsed.</param>
    /// <returns>True if successful, false for error (skip to next schedule entry).</returns>
    public static bool TryFindGOTOschedule(NPC npc, SDate date, string rawData, out string scheduleString)
    {
        scheduleString = string.Empty;
        string[] splits = rawData.Split(
            separator: '/',
            count: 3,
            options: StringSplitOptions.TrimEntries);
        string[] command = splits[0].Split();
        switch (command[0])
        {
            case "GOTO":
                // GOTO NO_SCHEDULE
                if (command[1].Equals("NO_SCHEDULE", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                string newKey = command[1];
                // GOTO season
                if (newKey.Equals("Season", StringComparison.OrdinalIgnoreCase))
                {
                    newKey = date.Season.ToLowerInvariant();
                }
                // GOTO newKey
                if (npc.hasMasterScheduleEntry(newKey))
                {
                    string newscheduleKey = npc.getMasterScheduleEntry(newKey);
                    if (newscheduleKey.Equals(rawData, StringComparison.Ordinal))
                    {
                        Globals.ModMonitor.Log(I18n.GOTOINFINITELOOP(), LogLevel.Warn);
                        return false;
                    }
                    return TryFindGOTOschedule(npc, date, newscheduleKey, out scheduleString);
                }
                else
                {
                    Globals.ModMonitor.Log(I18n.GOTOSCHEDULENOTFOUND(newKey, npc.Name), LogLevel.Warn);
                    return false;
                }
            case "NOT":
                // NOT friendship NPCName heartLevel
                if (command[1].Equals("friendship", StringComparison.Ordinal))
                {
                    int hearts = Utility.GetAllPlayerFriendshipLevel(Game1.getCharacterFromName(command[2])) / 250;
                    if (!int.TryParse(command[3], out int heartLevel))
                    {
                        // ill formed friendship check string, warn
                        Globals.ModMonitor.Log(I18n.GOTOILLFORMEDFRIENDSHIP(splits[0], npc.Name, rawData), LogLevel.Warn);
                        return false;
                    }
                    else if (hearts > heartLevel)
                    {
                        // hearts above what's allowed, skip to next schedule.
                        Globals.ModMonitor.Log(I18n.GOTOSCHEDULEFRIENDSHIP(npc.Name, rawData), LogLevel.Trace);
                        return false;
                    }
                }
                scheduleString = rawData;
                return true;
            case "MAIL":
                // MAIL mailkey
                return Game1.MasterPlayer.mailReceived.Contains(command[1]) || NetWorldState.checkAnywhereForWorldStateID(command[1])
                    ? TryFindGOTOschedule(npc, date, splits[2], out scheduleString)
                    : TryFindGOTOschedule(npc, date, splits[1], out scheduleString);
            default:
                scheduleString = rawData;
                return true;
        }
    }

    /// <summary>
    /// Handles parsing a schedule for s chedule string already stripped of GOTO/MAIL/NOT.
    /// </summary>
    /// <param name="schedule">Raw schedule string.</param>
    /// <param name="npc">NPC.</param>
    /// <param name="prevMap">Map NPC starts on.</param>
    /// <param name="prevStop">Start location.</param>
    /// <param name="prevtime">Start time of scheduler.</param>
    /// <returns>null if the schedule could not be parsed, a schedule otherwise.</returns>
    /// <exception cref="MethodNotFoundException">Reflection to get game methods failed.</exception>
    /// <remarks>Does NOT set NPC.daySchedule - still need to set that manually if that's wanted.</remarks>
    public static Dictionary<int, SchedulePathDescription>? ParseSchedule(string? schedule, NPC npc, string prevMap, Point prevStop, int prevtime)
    {
        if (schedule is null)
        {
            return null;
        }

        string previousMap = prevMap;
        Point lastStop = prevStop;
        int lastx = lastStop.X;
        int lasty = lastStop.Y;
        int lasttime = prevtime;

        Dictionary<int, SchedulePathDescription> remainderSchedule = new();
        IReflectedMethod pathfinder = Globals.ReflectionHelper.GetMethod(npc, "pathfindToNextScheduleLocation")
            ?? throw new MethodNotFoundException("NPC::pathfindToNextScheduleLocation");

        WarpPoint? warpPoint = null;

        foreach (string schedulepoint in schedule.Split('/'))
        {
            try
            {
                Match match = Globals.ScheduleRegex.Match(schedulepoint);

                if (!match.Success)
                {
                    // Handle the case of <time> bed.
                    Match bedmatch = Globals.BedRegex.Match(schedulepoint);
                    if (bedmatch.Success)
                    {
                        Dictionary<string, string> bedmatchDict = bedmatch.MatchGroupsToDictionary((string key) => key, (string value) => value.Trim());

                        // grab the original time.
                        string bedtime = (bedmatchDict.TryGetValue("arrival", out string? bedarrival) && bedarrival.Equals("a", StringComparison.OrdinalIgnoreCase)) ?
                                bedarrival + bedmatchDict["time"] : bedmatchDict["time"];
                        if (npc.isMarried() || npc.DefaultMap.Equals("FarmHouse", StringComparison.OrdinalIgnoreCase))
                        {
                            match = Globals.ScheduleRegex.Match(bedtime + " BusStop -1 23 3");
                        }
                        else if (npc.TryGetScheduleEntry("default", out string? defaultSchedule) && GetLastPointWithoutTime(defaultSchedule) is string defaultbed)
                        {
                            match = Globals.ScheduleRegex.Match(bedtime + ' ' + defaultbed);
                        }
                        else if (npc.TryGetScheduleEntry("spring", out string? springSchedule) && GetLastPointWithoutTime(springSchedule) is string springbed)
                        {
                            match = Globals.ScheduleRegex.Match(bedtime + ' ' + springbed);
                        }
                    }
                }

                if (!match.Success)
                { // I still have issues, try sending the NPC straight home to bed.
                    Globals.ModMonitor.Log($"{schedulepoint} seems unparsable by regex, sending NPC {npc.Name} home to sleep", LogLevel.Info);

                    // If the NPC has a sleep animation, use it.
                    Dictionary<string, string> animationData = Globals.ContentHelper.Load<Dictionary<string, string>>("Data\\animationDescriptions", ContentSource.GameContent);
                    string? sleepanimation = npc.Name.ToLowerInvariant() + "_sleep";
                    sleepanimation = animationData.ContainsKey(sleepanimation) ? sleepanimation : null;
                    SchedulePathDescription path2bed = pathfinder.Invoke<SchedulePathDescription>(
                        previousMap,
                        lastx,
                        lasty,
                        npc.DefaultMap,
                        npc.DefaultPosition.X,
                        npc.DefaultPosition.Y,
                        Game1.up,
                        sleepanimation,
                        null); // no message.
                    string originaltime;
                    int spaceloc = schedulepoint.IndexOf(' ');
                    if (spaceloc == -1)
                    {
                        Globals.ModMonitor.Log($"Failed in parsing schedulepoint {schedulepoint} for NPC {npc.Name}", LogLevel.Warn);
                        return null; // to try next schedule for GIMA, to null out NPC schedule and give them no schedule for vanilla.
                    }
                    else
                    {
                        originaltime = schedulepoint[(spaceloc + 1) .. ];
                    }
                    if (int.TryParse(originaltime, out int path2bedtime))
                    {
                        if (path2bedtime < lasttime)
                        {
                            if (!Globals.Config.EnforceGITiming)
                            { // I've already adjusted the last time parameter to account for travel time
                                path2bedtime = Utility.ConvertMinutesToTime(((Utility.ConvertTimeToMinutes(lasttime) * 10) / 10) + 10);
                            }
                            else if (remainderSchedule.TryGetValue(lasttime, out SchedulePathDescription? lastschedpoint))
                            {
                                path2bedtime = Utility.ConvertMinutesToTime(((Utility.ConvertTimeToMinutes(lasttime) + lastschedpoint.GetExpectedRouteTime()) * 10 / 10) + 10);
                            }
                        }
                        remainderSchedule[path2bedtime] = path2bed;
                        return remainderSchedule;
                    }
                    else if (originaltime.Length > 0 && originaltime.StartsWith('a') && int.TryParse(originaltime[1..], out path2bedtime))
                    {
                        int expectedpath2bedtime = path2bed.GetExpectedRouteTime();
                        Utility.ModifyTime(path2bedtime, 0 - ((expectedpath2bedtime * 10) / 10));
                        if (path2bedtime < lasttime)
                        { // a little sanity checking, force the bed time to be sufficiently after the previous point.
                            if (!Globals.Config.EnforceGITiming)
                            { // I've already adjusted the last time parameter to account for travel time
                                path2bedtime = Utility.ConvertMinutesToTime(((Utility.ConvertTimeToMinutes(lasttime) * 10) / 10) + 10);
                            }
                            else if (remainderSchedule.TryGetValue(lasttime, out SchedulePathDescription? lastschedpoint))
                            {
                                path2bedtime = Utility.ConvertMinutesToTime(((Utility.ConvertTimeToMinutes(lasttime) + lastschedpoint.GetExpectedRouteTime()) * 10 / 10) + 10);
                            }
                        }
                        remainderSchedule[path2bedtime] = path2bed;
                        return remainderSchedule;
                    }
                    else
                    {
                        Globals.ModMonitor.Log($"Failed in parsing schedulepoint {schedulepoint} for NPC {npc.Name}", LogLevel.Warn);
                        return null; // to try next schedule for GIMA, to null out NPC schedule and give them no schedule for vanilla.
                    }
                }

                // Process a successful match
                Dictionary<string, string> matchDict = match.MatchGroupsToDictionary((key) => key, (value) => value.Trim());
                int time = int.Parse(matchDict["time"]);
                string location = matchDict.GetValueOrDefaultOverrideNull("location", previousMap);
                int x = int.Parse(matchDict["x"]);
                int y = int.Parse(matchDict["y"]);
                string direction_str = matchDict.GetValueOrDefault("direction", "2");

                if (!int.TryParse(direction_str, out int direction))
                {
                    direction = Game1.down;
                }

                // Adjust schedules for locations not being open....
                if (!Game1.isLocationAccessible(location))
                {
                    if (npc.TryGetScheduleEntry(location + "_Replacement", out string? replacement))
                    {
                        string[] replacementdata = replacement.Split();
                        x = int.Parse(replacementdata[0]);
                        y = int.Parse(replacementdata[1]);
                        if (!int.TryParse(replacementdata[2], out direction))
                        {
                            direction = Game1.down;
                        }
                    }
                    else
                    {
                        if (Globals.Config.EnforceGITiming)
                        {
                            Globals.ModMonitor.Log(I18n.NOREPLACEMENTLOCATION(location, npc.Name), LogLevel.Warn);
                        }
                        continue; // skip this schedule point
                    }
                }

                if (time == 0)
                {
                    warpPoint = new(location, new Vector2(x, y));
                    continue; // zero points are just to set warps, do not add to schedule.
                }
                else if (time <= lasttime)
                {
                    Globals.ModMonitor.Log(I18n.TOOTIGHTTIMELINE(time, schedule, npc.Name), LogLevel.Warn);
                    continue;
                }

                matchDict.TryGetValue("animation", out string? animation);
                matchDict.TryGetValue("message", out string? message);

                SchedulePathDescription newpath = pathfinder.Invoke<SchedulePathDescription>(
                    previousMap,
                    lastx,
                    lasty,
                    location,
                    x,
                    y,
                    direction,
                    animation,
                    message);

                if (matchDict.TryGetValue("arrival", out string? arrival) && arrival.Equals("a", StringComparison.OrdinalIgnoreCase))
                {
                    time = Utility.ModifyTime(time, -(newpath.GetExpectedRouteTime() * 10 / 10));
                }
                if (time <= lasttime)
                {
                    Globals.ModMonitor.Log(I18n.TOOTIGHTTIMELINE(time, schedule, npc.Name), LogLevel.Warn);
                    continue; // skip to next point.
                }
                Globals.ModMonitor.DebugLog($"Adding GI schedule for {npc.Name}", LogLevel.Debug);
                remainderSchedule.Add(time, newpath);
                previousMap = location;
                lasttime = time;
                lastx = x;
                lasty = y;
                if (Globals.Config.EnforceGITiming)
                {
                    int expectedTravelTime = newpath.GetExpectedRouteTime();
                    Utility.ModifyTime(lasttime, expectedTravelTime);
                    Globals.ModMonitor.DebugLog($"Expected travel time of {expectedTravelTime} minutes", LogLevel.Debug);
                }
            }
            catch (RegexMatchTimeoutException ex)
            {
                Globals.ModMonitor.Log(I18n.REGEXTIMEOUTERROR(schedulepoint, ex), LogLevel.Trace);
                continue;
            }
        }

        if (remainderSchedule.Count > 0)
        {
            if (warpPoint is not null)
            {
                Game1.warpCharacter(npc, warpPoint.MapName, warpPoint.Location);
            }
            return remainderSchedule;
        }

        return null;
    }

    /// <summary>
    /// Wraps npc.parseMasterSchedule to lie to it about the start location of the NPC, if the NPC lives in the farmhouse.
    /// </summary>
    /// <param name="npc">NPC in question.</param>
    /// <param name="rawData">Raw schedule string.</param>
    public static void ParseMasterScheduleAdjustedForChild2NPC(NPC npc, string rawData)
    {
        if (Globals.IsChildToNPC?.Invoke(npc) == true)
        {
            // For a Child2NPC, we must handle their scheduling ourselves.
            if (TryFindGOTOschedule(npc, SDate.Now(), rawData, out string scheduleString))
            {
                npc.Schedule = ParseSchedule(scheduleString, npc, "BusStop", new Point(0, 23), 610);
                if (Context.IsMainPlayer && npc.Schedule is not null
                    && Globals.ReflectionHelper.GetField<string>(npc, "_lastLoadedScheduleKey", false)?.GetValue() is string lastschedulekey)
                {
                    npc.dayScheduleName.Value = lastschedulekey;
                }
            }
            else
            {
                Globals.ModMonitor.Log("TryFindGOTOschedule failed for Child2NPC, setting schedule to null", LogLevel.Warn);
                npc.Schedule = null;
            }
        }
        else if (npc.DefaultMap.Equals("FarmHouse", StringComparison.OrdinalIgnoreCase) && !npc.isMarried())
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
            try
            {
                npc.Schedule = npc.parseMasterSchedule(rawData);
            }
            catch (Exception ex)
            {
                Globals.ModMonitor.Log($"Ran into issues parsing schedule {rawData} for {npc.Name}.\n\n{ex}", LogLevel.Error);
            }
            finally
            {
                npc.DefaultMap = prevmap;
                npc.DefaultPosition = prevposition;
            }

            ScheduleUtilities.Schedules[npc.Name] = npc.Schedule;
        }
        else
        {
            npc.Schedule = npc.parseMasterSchedule(rawData);
        }
    }

    /// <summary>
    /// Goes around at about 620 and fixes up schedules if they've been nulled.
    /// Child2NPC may try to fix up their children around ~610 AM. Sadly, that nulls the schedules.
    /// </summary>
    public static void FixUpSchedules()
    {
        foreach (NPC npc in Game1.getLocationFromName("FarmHouse").getCharacters())
        {
            if (Globals.IsChildToNPC?.Invoke(npc) == true && npc.Schedule is null
                && ScheduleUtilities.Schedules.TryGetValue(npc.Name, out Dictionary<int, SchedulePathDescription>? schedule))
            {
                Globals.ModMonitor.Log($"Fixing up schedule for {npc.Name}, which appears to have been nulled.", LogLevel.Warn);
                npc.Schedule = schedule;
                ScheduleUtilities.Schedules.Remove(npc.Name);
            }
        }
    }

    /// <summary>
    /// Given an schedule, returns the last schedule point without the time.
    /// </summary>
    /// <param name="rawSchedule">Raw schedule string.</param>
    /// <returns>Last schedule point without the time, or null for failure.</returns>
    private static string? GetLastPointWithoutTime(string rawSchedule)
    {
        int slashloc = rawSchedule.LastIndexOf('/');
        if (slashloc > 0)
        {
            string lastentry = rawSchedule[(slashloc + 1) .. ];
            int spaceloc = lastentry.IndexOf(' ');
            if (spaceloc > 0)
            {
                return lastentry[(spaceloc + 1) .. ];
            }
        }
        return null;
    }
}