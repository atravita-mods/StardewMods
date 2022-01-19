using GingerIslandMainlandAdjustments.ScheduleManager.DataModels;
using GingerIslandMainlandAdjustments.Utils;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Utilities;
using StardewValley.Locations;

namespace GingerIslandMainlandAdjustments.ScheduleManager;

/// <summary>
/// Class that handles scheduling if the <see cref="ModConfig.UseThisScheduler"/> option is set.
/// </summary>
internal class GIScheduler
{
    /// <summary>
    /// Instance of a class than handles finding the schedules.
    /// </summary>
    private static readonly ScheduleManager ScheduleManager = new();

    private static readonly int[] TIMESLOTS = new int[] { 1200, 1400, 1600 };

    /// <summary>
    /// IslandNorth points for the adventurous.
    /// </summary>
    private static readonly List<Point> AdventurousPoint = new()
    {
        new Point(33, 83),
        new Point(41, 74),
        new Point(48, 81),
    };

    /// <summary>
    /// Dictionary of possible island groups. Null is a cache miss.
    /// </summary>
    /// <remarks>Use the getter, which will automatically grab from fake asset.</remarks>
    private static Dictionary<string, HashSet<NPC>>? islandGroups = null;

    /// <summary>
    /// Dictionary of possible explorer groups. Null is a cache miss.
    /// </summary>
    /// <remarks>Use the getter, which will automatically grab from fake asset.</remarks>
    private static Dictionary<string, HashSet<NPC>>? explorers = null;

    private static NPC? bartender;

    private static NPC? musician;

    /// <summary>
    /// Gets or sets the current group headed off to the island.
    /// </summary>
    /// <remarks>null means no current group.</remarks>
    public static string? CurrentGroup { get; set; }

    /// <summary>
    /// Gets the current bartender.
    /// </summary>
    public static NPC? Bartender => bartender;

    /// <summary>
    /// Gets the current musician.
    /// </summary>
    public static NPC? Musician => musician;

    /// <summary>
    /// Gets island groups. Will automatically load if null.
    /// </summary>
    private static Dictionary<string, HashSet<NPC>> IslandGroups
    {
        get
        {
            if (islandGroups is null)
            {
                islandGroups = AssetManager.GetCharacterGroup(SpecialGroupType.Groups);
            }
            return islandGroups;
        }
    }

    /// <summary>
    /// Gets explorer groups. Will automatically load if null.
    /// </summary>
    private static Dictionary<string, HashSet<NPC>> Explorers
    {
        get
        {
            if (explorers is null)
            {
                explorers = AssetManager.GetCharacterGroup(SpecialGroupType.Explorers);
            }
            return explorers;
        }
    }

    /// <summary>
    /// Clears the cached values for this class.
    /// </summary>
    public static void ClearCache()
    {
        islandGroups = null;
        explorers = null;
    }

    /// <summary>
    /// Generates schedules for everyone.
    /// </summary>
    public static void GenerateAllSchedules()
    {
        Game1.netWorldState.Value.IslandVisitors.Clear();
        if (Utility.isFestivalDay(Game1.Date.DayOfMonth, Game1.Date.Season)
            || (Game1.Date.Season.Equals("winter", StringComparison.OrdinalIgnoreCase) && Game1.Date.DayOfMonth >= 15 && Game1.Date.DayOfMonth <= 17)
            || Game1.getLocationFromName("IslandSouth") is not IslandSouth island || !island.resortRestored.Value
            || Game1.IsRainingHere(island) || !island.resortOpenToday.Value)
        {
            return;
        }
        Random random = new((int)((float)Game1.uniqueIDForThisGame * 1.21f) + (int)((float)Game1.stats.DaysPlayed * 2.5f));
        Dictionary<string, string> animationDescriptions = Globals.ContentHelper.Load<Dictionary<string, string>>("Data/animationDescriptions");

        HashSet<NPC> explorers = GenerateExplorerGroup(random);

        List<NPC> visitors = GenerateVistorList(random, Globals.Config.Capacity, explorers);

        GIScheduler.bartender = SetBartender(visitors);
        GIScheduler.musician = SetMusician(random, visitors, animationDescriptions);

        List<GingerIslandTimeSlot> activities = AssignIslandSchedules(random, visitors, animationDescriptions);
        Dictionary<NPC, string> schedules = RenderIslandSchedules(random, visitors, activities);

        int explorerIndex = 0;
        if (explorers.Any())
        {
            foreach (NPC explorer in explorers)
            {
                SchedulePoint schedulePoint = new(
                    random: random,
                    npc: explorer,
                    map: "IslandNorth",
                    time: 1400,
                    point: GIScheduler.AdventurousPoint[explorerIndex++],
                    isarrivaltime: true,
                    basekey: "Resort_Adventure",
                    direction: explorerIndex);
                schedules[explorer] = schedulePoint.ToString() + "/" + (ScheduleManager.FindProperGISchedule(explorer, SDate.Now()) ?? "1800 bed");
            }
        }

        foreach (NPC visitor in schedules.Keys)
        {
            Globals.ModMonitor.Log($"Calculated island schedule for {visitor.Name}");
            visitor.islandScheduleName.Value = "island";
            visitor.Schedule = visitor.parseMasterSchedule(schedules[visitor]);
            Game1.netWorldState.Value.IslandVisitors[visitor.Name] = true;
        }
    }

    /// <summary>
    /// Yields a group of valid explorers.
    /// </summary>
    /// <param name="random">Seeded random.</param>
    /// <returns>An explorer group, or an empty hashset if there's no group today.</returns>
    private static HashSet<NPC> GenerateExplorerGroup(Random random)
    {
        if (random.NextDouble() <= Globals.Config.ExplorerChance)
        {
            List<string> explorerGroups = Explorers.Keys.ToList();
            if (explorerGroups.Count > 0)
            {
                string explorerGroup = explorerGroups[random.Next(explorerGroups.Count)];
                return Explorers[explorerGroup].Where((NPC npc) => IslandSouth.CanVisitIslandToday(npc)).Take(3).ToHashSet();
            }
        }
        return new HashSet<NPC>(); // just return an empty hashset.
    }

    /// <summary>
    /// Gets the visitor list for a specific day. Explorers can't be visitors, so remove them.
    /// </summary>
    /// <param name="random">Random to use to select.</param>
    /// <param name="capacity">Maximum number of people to allow on the island.</param>
    /// <returns>Visitor List.</returns>
    /// <remarks>For a deterministic island list, use a Random seeded with the uniqueID + number of days played.</remarks>
    private static List<NPC> GenerateVistorList(Random random, int capacity, HashSet<NPC> explorers)
    {
        List<NPC> visitors = new();
        HashSet<NPC> valid_visitors = new();

        foreach (NPC npc in Utility.getAllCharacters())
        {
            if (IslandSouth.CanVisitIslandToday(npc) && !explorers.Contains(npc))
            {
                valid_visitors.Add(npc);
            }
        }
        if (random.NextDouble() < 0.6)
        {
            List<string> groupkeys = new();
            foreach (string key in IslandGroups.Keys)
            {
                if (IslandGroups[key].All((NPC npc) => IslandSouth.CanVisitIslandToday(npc)))
                {
                    groupkeys.Add(key);
                }
            }
            CurrentGroup = Utility.GetRandom(groupkeys, random);
            HashSet<NPC> possiblegroup = IslandGroups[CurrentGroup];
            visitors = possiblegroup.OrderBy(a => random.Next()).Take(capacity).ToList();
            valid_visitors.ExceptWith(visitors);
        }
        NPC? gus = Game1.getCharacterFromName("Gus");
        if (gus is not null && !visitors.Contains(gus) && !explorers.Contains(gus)
            && Globals.Config.GusDayAsShortString().Equals(Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth), StringComparison.OrdinalIgnoreCase)
            && Globals.Config.GusChance < random.NextDouble())
        {
            Globals.ModMonitor.DebugLog($"Forcibly adding Gus");
            if (!visitors.Contains(gus))
            {
                visitors.Add(gus);
                valid_visitors.Remove(gus);
            }
        }
        if (visitors.Count < capacity)
        {
            foreach (NPC newvisitor in valid_visitors.OrderBy(a => random.Next()).Take(capacity - visitors.Count))
            {
                visitors.Add(newvisitor);
            }
        }
        for (int i = 0; i < visitors.Count; i++)
        {
            visitors[i].scheduleDelaySeconds = Math.Min(i * 0.4f, 7f);
        }
        Globals.ModMonitor.DebugLog($"{visitors.Count} vistors: {string.Join(", ", visitors)}");
        return visitors;
    }

    /// <summary>
    /// Returns either Gus if he's visiting, or a valid bartender from the bartender list.
    /// </summary>
    /// <param name="visitors">List of possible visitors for the day.</param>
    /// <returns>Bartender if it can find one, null otherwise.</returns>
    private static NPC? SetBartender(List<NPC> visitors)
    {
        NPC? bartender = visitors.Find((NPC npc) => npc.Name.Equals("Gus"));
        if (bartender is null)
        { // Gus not visiting, go find another bartender
            HashSet<NPC> bartenders = AssetManager.GetSpecialCharacter(SpecialCharacterType.Bartender);
            bartender = visitors.Find((NPC npc) => bartenders.Contains(npc));
        }
        if (bartender != null)
        {
            bartender.currentScheduleDelay = 0f;
        }
        return bartender;
    }

    /// <summary>
    /// Returns a possible musician. Prefers Sam.
    /// </summary>
    /// <param name="random">The seeded random.</param>
    /// <param name="visitors">List of visitors.</param>
    /// <param name="animationDescriptions">Animation descriptions dictionary (pass this in to avoid rereading it).</param>
    /// <returns>Musician if it finds one.</returns>
    private static NPC? SetMusician(Random random, List<NPC> visitors, Dictionary<string, string> animationDescriptions)
    {
        NPC? musician = visitors.Find((NPC npc) => npc.Name.Equals("Sam") && animationDescriptions.ContainsKey("sam_beach_towel"));
        if (musician is null || random.NextDouble() < 0.25)
        {
            HashSet<NPC> musicians = AssetManager.GetSpecialCharacter(SpecialCharacterType.Musician);
            musician = visitors.Find((NPC npc) => musicians.Contains(npc) && animationDescriptions.ContainsKey($"{npc.Name.ToLowerInvariant()}_beach_towel"));
        }
        if (musician is not null && !musician.Name.Equals("Gus", StringComparison.OrdinalIgnoreCase))
        {
            musician.currentScheduleDelay = 0f;
            return musician;
        }
        return null;
    }

    /// <summary>
    /// Assigns everyone their island schedules for the day.
    /// </summary>
    /// /// <param name="random">Seeded random.</param>
    /// <param name="visitors">List of visitors.</param>
    /// <returns>A list of filled <see cref="GingerIslandTimeSlot"/>s.</returns>
    private static List<GingerIslandTimeSlot> AssignIslandSchedules(Random random, List<NPC> visitors, Dictionary<string, string> animationDescriptions)
    {
        Dictionary<NPC, string> lastactivity = new();
        List<GingerIslandTimeSlot> activities = TIMESLOTS.Select((i) => new GingerIslandTimeSlot(i, Bartender, Musician, random, visitors)).ToList();

        foreach (GingerIslandTimeSlot activity in activities)
        {
            activity.AssignActivities(lastactivity, animationDescriptions);
        }

        return activities;
    }

    /// <summary>
    /// Takes a list of activities and renders them as proper schedules.
    /// </summary>
    /// <param name="random">Sedded random.</param>
    /// <param name="visitors">List of visitors.</param>
    /// <param name="activities">List of activities.</param>
    /// <returns>Dictionary of NPC->raw schedule strings.</returns>
    private static Dictionary<NPC, string> RenderIslandSchedules(Random random, List<NPC> visitors, List<GingerIslandTimeSlot> activities)
    {
        Dictionary<NPC, string> completedSchedules = new();

        foreach (NPC visitor in visitors)
        {
            bool should_dress = IslandSouth.HasIslandAttire(visitor);
            List<SchedulePoint> scheduleList = new();

            if (should_dress)
            {
                scheduleList.Add(new SchedulePoint(
                    random: random,
                    npc: visitor,
                    map: "IslandSouth",
                    time: 1150,
                    point: IslandSouth.GetDressingRoomPoint(visitor),
                    animation: "change_beach",
                    isarrivaltime: true));
            }

            foreach (GingerIslandTimeSlot activity in activities)
            {
                if (activity.Assignments.TryGetValue(visitor, out SchedulePoint? schedulePoint))
                {
                    scheduleList.Add(schedulePoint);
                }
            }

            if (should_dress)
            {
                scheduleList.Add(new SchedulePoint(
                    random: random,
                    npc: visitor,
                    map: "IslandSouth",
                    time: 1730,
                    point: IslandSouth.GetDressingRoomPoint(visitor),
                    animation: "change_normal",
                    isarrivaltime: true));
            }

            scheduleList[0].IsArrivalTime = true; // set the first slot, whatever it is, to be the arrival time.

            // render the schedule points to strings before appending the remainder schedules
            // which are already strings.
            List<string> schedPointString = scheduleList.Select((SchedulePoint pt) => pt.ToString()).ToList();
            if (visitor.Name.Equals("Gus", StringComparison.InvariantCultureIgnoreCase))
            {
                // Gus needs to tend bar. Hardcoded same as vanilla.
                schedPointString.Add("1800 Saloon 10 18 2/2430 bed");
            }
            else
            {
                // Try to find a GI remainder schedule, if any.
                schedPointString.Add(ScheduleManager.FindProperGISchedule(visitor, SDate.Now()) ?? "1800 bed");
            }
            completedSchedules[visitor] = string.Join("/", schedPointString);
            Globals.ModMonitor.DebugLog($"For {visitor.Name}, created island schedule {completedSchedules[visitor]}");
        }
        return completedSchedules;
    }
}