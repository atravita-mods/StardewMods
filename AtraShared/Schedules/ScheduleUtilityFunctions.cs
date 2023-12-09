// Ignore Spelling: Loc Qual prevtime Oschedule npc

using System.Text.RegularExpressions;

using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;
using AtraBase.Toolkit.StringHandler;

using AtraCore.Framework.Caches;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Utilities;

using StardewValley.Network;
using StardewValley.Pathfinding;

namespace AtraShared.Schedules;

/// <summary>
/// Holds a map + location.
/// </summary>
/// <param name="MapName">Map name as string.</param>
/// <param name="Location">Vector2 location to warp to.</param>
internal record struct QualLoc(string MapName, Vector2 Location)
{
}

/// <summary>
/// Class that holds scheduling utility functions.
/// </summary>
/// <param name="monitor">The logger.</param>
/// <param name="translation">The translation helper.</param>
public class ScheduleUtilityFunctions(
    IMonitor monitor,
    ITranslationHelper translation)
{
    /// <summary>
    /// Regex for a schedulepoint format.
    /// </summary>
    [RegexPattern]
    public static readonly Regex ScheduleRegex = new(
        // <time> [location] <tileX> <tileY> [facingDirection] [animation] \"[dialogue]\"
        pattern: @"(?<arrival>a)?(?<time>[0-9]{1,4})(?<location> \S+)*?(?<x> [0-9]{1,4})(?<y> [0-9]{1,4})(?<direction> [0-9])?(?<animation> [^\s\""]+)?(?<dialogue> \"".*\"")?",
        options: RegexOptions.CultureInvariant | RegexOptions.Compiled,
        matchTimeout: TimeSpan.FromMilliseconds(250));

    /// <summary>
    /// Regex that handles the bed location special case.
    /// </summary>
    [RegexPattern]
    public static readonly Regex BedRegex = new(
        // <time> bed
        pattern: @"(?<arrival>a)?(?<time>[0-9]{1,4}) bed",
        options: RegexOptions.CultureInvariant | RegexOptions.Compiled,
        matchTimeout: TimeSpan.FromMilliseconds(250));

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
                    newKey = date.SeasonKey.ToLowerInvariant();
                }

                // GOTO newKey
                if (npc.hasMasterScheduleEntry(newKey))
                {
                    string newScheduleKey = npc.getMasterScheduleEntry(newKey);
                    if (newScheduleKey.Equals(rawData, StringComparison.Ordinal))
                    {
                        monitor.Log(translation.Get("GOTO_INFINITE_LOOP").Default("Infinite loop detected, skipping this schedule."), LogLevel.Warn);
                        return false;
                    }
                    return this.TryFindGOTOschedule(npc, date, newScheduleKey, out scheduleString);
                }
                else
                {
                    monitor.Log(
                        translation.Get("GOTO_SCHEDULE_NOT_FOUND")
                        .Default("GOTO {{scheduleKey}} not found for NPC {{npc}}")
                        .Tokens(new { scheduleKey = newKey, npc = npc.Name }), LogLevel.Warn);
                    return false;
                }
            case "NOT":
                // NOT friendship NPCName heartLevel
                if (command[1].Equals("friendship", StringComparison.Ordinal))
                {
                    NPC? friendNpc = NPCCache.GetByVillagerName(command[2]);
                    if (friendNpc is null)
                    {
                        // can't find the friend npc.
                        monitor.Log(
                            translation.Get("GOTO_FRIEND_NOT_FOUND")
                            .Default("NPC {{npc}} not found, friend requirement {{requirment}} cannot be evaluated: {{scheduleKey}}")
                            .Tokens(new { npc = command[2], requirment = splits[0], schedulekey = rawData }), LogLevel.Warn);
                        return false;
                    }

                    int hearts = Utility.GetAllPlayerFriendshipLevel(friendNpc) / 250;
                    if (!int.TryParse(command[3], out int heartLevel))
                    {
                        // ill formed friendship check string, warn
                        monitor.Log(
                            translation.Get("GOTO_ILL_FORMED_FRIENDSHIP")
                            .Default("Ill-formed friendship requirment {{requirment}} for {{npc}}: {{scheduleKey}}")
                            .Tokens(new { requirment = splits[0], npc = npc.Name, scheduleKey = rawData }), LogLevel.Warn);
                        return false;
                    }
                    else if (hearts > heartLevel)
                    {
                        // hearts above what's allowed, skip to next schedule.
                        monitor.Log(
                            translation.Get("GOTO_SCHEDULE_FRIENDSHIP")
                            .Default("Skipping due to friendship limit for {{npc}}: {{scheduleKey}}")
                            .Tokens(new { npc = npc.Name, scheduleKey = rawData }), LogLevel.Trace);
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
    /// <param name="scheduleKey">The schedule key</param>
    /// <param name="schedule">Raw schedule string.</param>
    /// <param name="npc">NPC.</param>
    /// <param name="prevMap">Map NPC starts on.</param>
    /// <param name="prevStop">Start location. (if null, is the NPC's default map.)</param>
    /// <param name="prevtime">Start time of scheduler.</param>
    /// <param name="enforceStrictTiming">Whether or not to emit warnings and skip too-tight schedule points.</param>
    /// <returns>null if the schedule could not be parsed, a schedule otherwise.</returns>
    /// <exception cref="MethodNotFoundException">Reflection to get game methods failed.</exception>
    /// <remarks>Does NOT set NPC.daySchedule - still need to set that manually if that's wanted.</remarks>
    public Dictionary<int, SchedulePathDescription>? ParseSchedule(
        string scheduleKey,
        string? schedule,
        NPC npc,
        string? prevMap = null,
        Point? prevStop = null,
        int prevtime = 610,
        bool enforceStrictTiming = false)
    {
        if (schedule is null)
        {
            return null;
        }

        string previousMap = prevMap ?? npc.DefaultMap;
        Point lastStop = prevStop ?? (npc.DefaultPosition / 64f).ToPoint();
        int lastx = lastStop.X;
        int lasty = lastStop.Y;
        int lasttime = prevtime;

        Dictionary<int, SchedulePathDescription> remainderSchedule = [];
        QualLoc? warpPoint = null;

        foreach (string schedulepoint in schedule.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
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
                        else if (npc.TryGetScheduleEntry("default", out string? defaultSchedule)
                            && GetLastPointWithoutTime(defaultSchedule) is ReadOnlySpan<char> defaultbed
                            && defaultbed.Length != 0)
                        {
                            match = ScheduleRegex.Match($"{bedtime} {defaultbed.ToString()}");
                        }
                        else if (npc.TryGetScheduleEntry("spring", out string? springSchedule)
                            && GetLastPointWithoutTime(springSchedule) is ReadOnlySpan<char> springbed
                            && springbed.Length != 0)
                        {
                            match = ScheduleRegex.Match($"{bedtime} {springbed.ToString()}");
                        }
                    }
                }

                if (!match.Success)
                { // I still have issues, try sending the NPC straight home to bed.
                    monitor.Log(
                        translation.Get("SCHEDULE_REGEX_FAILURE")
                        .Default("{{schedulepoint}} seems unparsable by regex, sending NPC {{npc}} home to sleep")
                        .Tokens(new { schedulepoint, npc = npc.Name }), LogLevel.Info);

                    // If the NPC has a sleep animation, use it.
                    Dictionary<string, string> animationData = DataLoader.AnimationDescriptions(Game1.content);
                    string? sleepanimation = npc.Name.ToLowerInvariant() + "_sleep";
                    sleepanimation = animationData.ContainsKey(sleepanimation) ? sleepanimation : null;
                    SchedulePathDescription path2bed = npc.pathfindToNextScheduleLocation(
                        scheduleKey,
                        previousMap,
                        lastx,
                        lasty,
                        npc.DefaultMap,
                        (int)npc.DefaultPosition.X / 64,
                        (int)npc.DefaultPosition.Y / 64,
                        Game1.up,
                        sleepanimation,
                        null);
                    string originaltime;
                    int spaceloc = schedulepoint.IndexOf(' ');
                    if (spaceloc == -1)
                    {
                        monitor.Log(
                            translation.Get("SCHEDULE_PARSE_FAILURE")
                            .Default("Failed in parsing schedulepoint {{schedulepoint}} for NPC {{npc}}")
                            .Tokens(new { schedulepoint, npc = npc.Name }), LogLevel.Warn);
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
                            if (!enforceStrictTiming)
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
                            if (!enforceStrictTiming)
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
                        monitor.Log(
                            translation.Get("SCHEDULE_PARSE_FAILURE")
                            .Default("Failed in parsing schedulepoint {{schedulepoint}} for NPC {{npc}}")
                            .Tokens(new { schedulepoint, npc = npc.Name }), LogLevel.Warn);
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
                        StreamSplit replacementdata = replacement.StreamSplit();

                        if (!replacementdata.MoveNext() || !int.TryParse(replacementdata.Current, out x)
                            || !replacementdata.MoveNext() || !int.TryParse(replacementdata.Current, out y))
                        {
                            monitor.Log($"Failed in parsing replacement {replacement}", LogLevel.Warn);
                            continue;
                        }
                        if (!replacementdata.MoveNext() || !int.TryParse(replacementdata.Current, out direction))
                        {
                            direction = Game1.down;
                        }
                    }
                    else
                    {
                        if (enforceStrictTiming)
                        {
                            monitor.Log(
                                translation.Get("NO_REPLACEMENT_LOCATION")
                                .Default("Location replacement for {{location}} requested but not found for {{npc}}")
                                .Tokens(new { location, npc=npc.Name }), LogLevel.Warn);
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
                    monitor.Log(
                        translation.Get("TOO_TIGHT_TIMELINE")
                        .Default("{{time}} position in schedule {{scheduleKey}} for {{npc}} is too tight. Will be skipped.")
                        .Tokens(new { time, scheduleKey = schedule, npc = npc.Name }), LogLevel.Warn);
                    continue;
                }

                matchDict.TryGetValue("animation", out string? animation);
                matchDict.TryGetValue("message", out string? message);

                SchedulePathDescription newpath = npc.pathfindToNextScheduleLocation(
                    scheduleKey,
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
                    monitor.Log(
                        translation.Get("TOO_TIGHT_TIMELINE")
                        .Default("{{time}} position in schedule {{scheduleKey}} for {{npc}} is too tight. Will be skipped.")
                        .Tokens(new { time, scheduleKey = schedule, npc = npc.Name }), LogLevel.Warn);
                    continue; // skip to next point.
                }
                monitor.DebugOnlyLog($"Adding GI schedule for {npc.Name}", LogLevel.Debug);
                remainderSchedule.Add(time, newpath);
                previousMap = location;
                lasttime = time;
                lastx = x;
                lasty = y;
                if (enforceStrictTiming)
                {
                    int expectedTravelTime = newpath.GetExpectedRouteTime();
                    Utility.ModifyTime(lasttime, expectedTravelTime);
                    monitor.DebugOnlyLog($"Expected travel time of {expectedTravelTime} minutes", LogLevel.Debug);
                }
            }
            catch (RegexMatchTimeoutException ex)
            {
                monitor.Log(
                    translation.Get("REGEX_TIMEOUT_ERROR")
                    .Default("Regex for schedule entry {{schedulePoint}} timed out:\n\n{{ex}}")
                    .Tokens(new { schedulePoint = schedulepoint, ex }), LogLevel.Warn);
                continue;
            }
        }

        if (remainderSchedule.Count > 0)
        {
            if (warpPoint is not null)
            {
                Game1.warpCharacter(npc, warpPoint.Value.MapName, warpPoint.Value.Location);
            }
            return remainderSchedule;
        }

        return null;
    }

    /// <summary>
    /// Given an schedule, returns the last schedule point without the time.
    /// </summary>
    /// <param name="rawSchedule">Raw schedule string.</param>
    /// <returns>Last schedule point without the time, or empty span for failure.</returns>
    private static ReadOnlySpan<char> GetLastPointWithoutTime(string rawSchedule)
    {
        ReadOnlySpan<char> lastPoint = rawSchedule.AsSpan().TrimEnd('/');
        int slashloc = rawSchedule.LastIndexOf('/');
        if (slashloc > 0)
        {
            lastPoint = rawSchedule.AsSpan(slashloc + 1);
        }

        if (lastPoint.TrySplitOnce(' ', out _, out ReadOnlySpan<char> second))
        {
            return second;
        }
        return ReadOnlySpan<char>.Empty;
    }
}