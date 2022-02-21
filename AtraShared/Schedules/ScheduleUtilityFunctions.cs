using System.Text.RegularExpressions;
using AtraBase.Utils.Extensions;
using AtraShared.Utils.Extensions;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Utilities;
using StardewValley.Network;

namespace AtraShared.Schedules;

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
/// Class that holds scheduling uility functions.
/// </summary>
internal class ScheduleUtilityFunctions
{
    /// <summary>
    /// Regex for a schedulepoint format.
    /// </summary>
    [RegexPattern]
    internal static readonly Regex ScheduleRegex = new(
        // <time> [location] <tileX> <tileY> [facingDirection] [animation] \"[dialogue]\"
        pattern: @"(?<arrival>a)?(?<time>[0-9]{1,4})(?<location> \S+)*?(?<x> [0-9]{1,4})(?<y> [0-9]{1,4})(?<direction> [0-9])?(?<animation> [^\s\""]+)?(?<dialogue> \"".*\"")?",
        options: RegexOptions.CultureInvariant | RegexOptions.Compiled,
        matchTimeout: TimeSpan.FromMilliseconds(250));

    /// <summary>
    /// Regex that handles the bed location special case.
    /// </summary>
    [RegexPattern]
    internal static readonly Regex BedRegex = new(
        // <time> bed
        pattern: @"(?<arrival>a)?(?<time>[0-9]{1,4}) bed",
        options: RegexOptions.CultureInvariant | RegexOptions.Compiled,
        matchTimeout: TimeSpan.FromMilliseconds(250));

#pragma warning disable SA1306 // Field names should begin with lower-case letter
    private readonly IMonitor monitor;
    private readonly IReflectionHelper reflectionHelper;
    private readonly Func<bool> GetStrictTiming;

    private readonly Func<string> GOTOINFINITELOOP;
    private readonly Func<string, string, string> GOTOSCHEDULENOTFOUND;
    private readonly Func<string, string, string, string> GOTOILLFORMEDFRIENDSHIP;
    private readonly Func<string, string, string> GOTOSCHEDULEFRIENDSHIP;
    private readonly Func<string, string, string> SCHEDULEPARSEFAILURE;
    private readonly Func<string, string, string> SCHEDULEREGEXFAILURE;
    private readonly Func<string, string, string> NOREPLACEMENTLOCATION;
    private readonly Func<string, string, string, string> TOOTIGHTTIMELINE;
    private readonly Func<string, string, string> REGEXTIMEOUTERROR;
#pragma warning restore SA1306 // Field names should begin with lower-case letter

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleUtilityFunctions"/> class.
    /// </summary>
    /// <param name="monitor">The logger.</param>
    /// <param name="reflectionHelper">The reflection helper.</param>
    /// <param name="getStrictTiming">Delegate: get whether strict timing is wanted.</param>
    /// <param name="gOTOINFINITELOOP">Translation delegate.</param>
    /// <param name="gOTOSCHEDULENOTFOUND">Translation delegate - schedule not found.</param>
    /// <param name="gOTOILLFORMEDFRIENDSHIP">Translation delegate - friendship unparsable.</param>
    /// <param name="gOTOSCHEDULEFRIENDSHIP">Translation delegate - skipping schedule due to friendship requiement.</param>
    /// <param name="sCHEDULEPARSEFAILURE">Translation delegate - schedule can't be parsed.</param>
    /// <param name="sCHEDULEREGEXFAILURE">Translation delegate - regex parsing failed.</param>
    /// <param name="nOREPLACEMENTLOCATION">Translation delegate - can't find a replacement location.</param>
    /// <param name="tOOTIGHTTIMELINE">Translation delegate - too tight timeline.</param>
    /// <param name="rEGEXTIMEOUTERROR">Translation delegate - regex timed out.</param>
    public ScheduleUtilityFunctions(
        IMonitor monitor,
        IReflectionHelper reflectionHelper,
        Func<bool> getStrictTiming,
        Func<string> gOTOINFINITELOOP,
        Func<string, string, string> gOTOSCHEDULENOTFOUND,
        Func<string, string, string, string> gOTOILLFORMEDFRIENDSHIP,
        Func<string, string, string> gOTOSCHEDULEFRIENDSHIP,
        Func<string, string, string> sCHEDULEPARSEFAILURE,
        Func<string, string, string> sCHEDULEREGEXFAILURE,
        Func<string, string, string> nOREPLACEMENTLOCATION,
        Func<string, string, string, string> tOOTIGHTTIMELINE,
        Func<string, string, string> rEGEXTIMEOUTERROR)
    {
        this.monitor = monitor;
        this.reflectionHelper = reflectionHelper;
        this.GetStrictTiming = getStrictTiming;
        this.GOTOINFINITELOOP = gOTOINFINITELOOP;
        this.GOTOSCHEDULENOTFOUND = gOTOSCHEDULENOTFOUND;
        this.GOTOILLFORMEDFRIENDSHIP = gOTOILLFORMEDFRIENDSHIP;
        this.GOTOSCHEDULEFRIENDSHIP = gOTOSCHEDULEFRIENDSHIP;
        this.SCHEDULEPARSEFAILURE = sCHEDULEPARSEFAILURE;
        this.SCHEDULEREGEXFAILURE = sCHEDULEREGEXFAILURE;
        this.NOREPLACEMENTLOCATION = nOREPLACEMENTLOCATION;
        this.TOOTIGHTTIMELINE = tOOTIGHTTIMELINE;
        this.REGEXTIMEOUTERROR = rEGEXTIMEOUTERROR;
    }

    /// <summary>
    /// Given a raw schedule string, returns a new raw schedule string, after following the GOTO/MAIL/NOT friendship keys in the game.
    /// </summary>
    /// <param name="npc">NPC.</param>
    /// <param name="date">The data to analyze.</param>
    /// <param name="rawData">The raw schedule string.</param>
    /// <param name="scheduleString">A raw schedule string, stripped of MAIL/GOTO/NOT elements. Ready to be parsed.</param>
    /// <returns>True if successful, false for error (skip to next schedule entry).</returns>
    public bool TryFindGOTOschedule(NPC npc, SDate date, string rawData, out string scheduleString)
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
                        this.monitor.Log(this.GOTOINFINITELOOP(), LogLevel.Warn);
                        return false;
                    }
                    return this.TryFindGOTOschedule(npc, date, newscheduleKey, out scheduleString);
                }
                else
                {
                    this.monitor.Log(this.GOTOSCHEDULENOTFOUND(newKey, npc.Name), LogLevel.Warn);
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
                        this.monitor.Log(this.GOTOILLFORMEDFRIENDSHIP(splits[0], npc.Name, rawData), LogLevel.Warn);
                        return false;
                    }
                    else if (hearts > heartLevel)
                    {
                        // hearts above what's allowed, skip to next schedule.
                        this.monitor.Log(this.GOTOSCHEDULEFRIENDSHIP(npc.Name, rawData), LogLevel.Trace);
                        return false;
                    }
                }
                scheduleString = rawData;
                return true;
            case "MAIL":
                // MAIL mailkey
                return Game1.MasterPlayer.mailReceived.Contains(command[1]) || NetWorldState.checkAnywhereForWorldStateID(command[1])
                    ? this.TryFindGOTOschedule(npc, date, splits[2], out scheduleString)
                    : this.TryFindGOTOschedule(npc, date, splits[1], out scheduleString);
            default:
                scheduleString = rawData;
                return true;
        }
    }

    /// <summary>
    /// Handles parsing a schedule for a schedule string already stripped of GOTO/MAIL/NOT.
    /// </summary>
    /// <param name="schedule">Raw schedule string.</param>
    /// <param name="npc">NPC.</param>
    /// <param name="prevMap">Map NPC starts on.</param>
    /// <param name="prevStop">Start location.</param>
    /// <param name="prevtime">Start time of scheduler.</param>
    /// <returns>null if the schedule could not be parsed, a schedule otherwise.</returns>
    /// <exception cref="MethodNotFoundException">Reflection to get game methods failed.</exception>
    /// <remarks>Does NOT set NPC.daySchedule - still need to set that manually if that's wanted.</remarks>
    public Dictionary<int, SchedulePathDescription>? ParseSchedule(string? schedule, NPC npc, string prevMap, Point prevStop, int prevtime)
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
        IReflectedMethod pathfinder = this.reflectionHelper.GetMethod(npc, "pathfindToNextScheduleLocation")
            ?? throw new MethodNotFoundException("NPC::pathfindToNextScheduleLocation");

        Func<string, int, int, string, int, int, int, string?, string?, SchedulePathDescription> pathfinderDelegate =
            (Func<string, int, int, string, int, int, int, string?, string?, SchedulePathDescription>)Delegate
            .CreateDelegate(typeof(Func<string, int, int, string, int, int, int, string?, string?, SchedulePathDescription>), pathfinder.MethodInfo);

        WarpPoint? warpPoint = null;

        foreach (string schedulepoint in schedule.Split('/'))
        {
            try
            {
                Match match = ScheduleRegex.Match(schedulepoint);

                if (!match.Success)
                {
                    // Handle the case of <time> bed.
                    Match bedmatch = BedRegex.Match(schedulepoint);
                    if (bedmatch.Success)
                    {
                        Dictionary<string, string> bedmatchDict = bedmatch.MatchGroupsToDictionary((string key) => key, (string value) => value.Trim());

                        // grab the original time.
                        string bedtime = (bedmatchDict.TryGetValue("arrival", out string? bedarrival) && bedarrival.Equals("a", StringComparison.OrdinalIgnoreCase)) ?
                                bedarrival + bedmatchDict["time"] : bedmatchDict["time"];
                        if (npc.isMarried() || npc.DefaultMap.Equals("FarmHouse", StringComparison.OrdinalIgnoreCase))
                        {
                            match = ScheduleRegex.Match(bedtime + " BusStop -1 23 3");
                        }
                        else if (npc.TryGetScheduleEntry("default", out string? defaultSchedule) && GetLastPointWithoutTime(defaultSchedule) is string defaultbed)
                        {
                            match = ScheduleRegex.Match(bedtime + ' ' + defaultbed);
                        }
                        else if (npc.TryGetScheduleEntry("spring", out string? springSchedule) && GetLastPointWithoutTime(springSchedule) is string springbed)
                        {
                            match = ScheduleRegex.Match(bedtime + ' ' + springbed);
                        }
                    }
                }

                if (!match.Success)
                { // I still have issues, try sending the NPC straight home to bed.
                    this.monitor.Log(this.SCHEDULEREGEXFAILURE(schedulepoint, npc.Name), LogLevel.Info);

                    // If the NPC has a sleep animation, use it.
                    Dictionary<string, string> animationData = Game1.content.Load<Dictionary<string, string>>("Data\\animationDescriptions");
                    string? sleepanimation = npc.Name.ToLowerInvariant() + "_sleep";
                    sleepanimation = animationData.ContainsKey(sleepanimation) ? sleepanimation : null;
                    SchedulePathDescription path2bed = pathfinderDelegate(
                        previousMap,
                        lastx,
                        lasty,
                        npc.DefaultMap,
                        (int)npc.DefaultPosition.X/64,
                        (int)npc.DefaultPosition.Y/64,
                        Game1.up,
                        sleepanimation,
                        null); // no message.
                    string originaltime;
                    int spaceloc = schedulepoint.IndexOf(' ');
                    if (spaceloc == -1)
                    {
                        this.monitor.Log(this.SCHEDULEPARSEFAILURE(schedulepoint, npc.Name), LogLevel.Warn);
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
                            if (!this.GetStrictTiming())
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
                            if (!this.GetStrictTiming())
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
                        this.monitor.Log(this.SCHEDULEPARSEFAILURE(schedulepoint, npc.Name), LogLevel.Warn);
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
                        if (this.GetStrictTiming())
                        {
                            this.monitor.Log(this.NOREPLACEMENTLOCATION(location, npc.Name), LogLevel.Warn);
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
                    this.monitor.Log(this.TOOTIGHTTIMELINE(time.ToString(), schedule, npc.Name), LogLevel.Warn);
                    continue;
                }

                matchDict.TryGetValue("animation", out string? animation);
                matchDict.TryGetValue("message", out string? message);

                SchedulePathDescription newpath = pathfinderDelegate(
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
                    this.monitor.Log(this.TOOTIGHTTIMELINE(time.ToString(), schedule, npc.Name), LogLevel.Warn);
                    continue; // skip to next point.
                }
                this.monitor.DebugLog($"Adding GI schedule for {npc.Name}", LogLevel.Debug);
                remainderSchedule.Add(time, newpath);
                previousMap = location;
                lasttime = time;
                lastx = x;
                lasty = y;
                if (this.GetStrictTiming())
                {
                    int expectedTravelTime = newpath.GetExpectedRouteTime();
                    Utility.ModifyTime(lasttime, expectedTravelTime);
                    this.monitor.DebugLog($"Expected travel time of {expectedTravelTime} minutes", LogLevel.Debug);
                }
            }
            catch (RegexMatchTimeoutException ex)
            {
                this.monitor.Log(this.REGEXTIMEOUTERROR(schedulepoint, ex.ToString()), LogLevel.Trace);
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