using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace NovaNPCTest;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    internal static IMonitor ModMonitor { get; private set; } = null!;

    internal static ScheduleUtilityFunctions Scheduler { get; set; } = null!;

    internal static Dictionary<string, int[]>? PortraitsToShake { get; private set; }

    internal static IReflectionHelper ReflectionHelper { get; private set; } = null!;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModMonitor = this.Monitor;
        Scheduler = new ScheduleUtilityFunctions(this.Monitor, this.Helper.Translation);
        ReflectionHelper = helper.Reflection;

        PortraitsToShake = helper.Data.ReadJsonFile<Dictionary<string, int[]>>("assets/portrait_shake.json");

        helper.Events.GameLoop.DayStarted += this.OnDayStart;

        this.ApplyPatches(new(this.ModManifest.UniqueID));
    }

    [EventPriority(EventPriority.Low - 10000)]
    private void OnDayStart(object? sender, DayStartedEventArgs e)
    {
        this.CheckSchedule(Game1.getCharacterFromName("Nova.Dylan"));
        this.CheckSchedule(Game1.getCharacterFromName("Nova.Eli"));
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll(typeof(ModEntry).Assembly);
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format("Mod crashed while applying harmony patches:\n\n{0}", ex), LogLevel.Error);
        }
    }

    private void CheckSchedule(NPC? npc)
    {
        if (npc is null)
        {
            return;
        }

        if (Game1.IsVisitingIslandToday(npc.Name) || npc.islandScheduleName.Value is not null)
        {
            ModMonitor.Log($"{npc.Name} is going to the island, have fun!");
            return;
        }

        ModMonitor.Log($"Checking {npc.Name}, is currently at {npc.currentLocation.NameOrUniqueName}");

        string? scheduleKey = npc.dayScheduleName.Value;

        if (scheduleKey is null)
        {
            npc.TryLoadSchedule();
            scheduleKey = npc.dayScheduleName.Value;
        }

        if (scheduleKey is null)
        {
            ModMonitor.Log($"{npc.Name} seems to be missing a schedule today, huh.");
            return;
        }

        if (scheduleKey.EndsWith("_Replacement"))
        {
            ModMonitor.Log($"{npc.Name} has a replacement schedule, probably shouldn't try to fix spawn points.");
            return;
        }

        if (!npc.TryGetScheduleEntry(scheduleKey, out string? entry))
        {
            ModMonitor.Log($"With schedule key {scheduleKey} that apparently doesn't correspond to a schedule. What.", LogLevel.Warn);
            return;
        }

        if (!Scheduler.TryFindGOTOschedule(npc, SDate.Now(), entry, out string? schedule))
        {
            ModMonitor.Log($"With schedule key {scheduleKey} that apparently doesn't correspond to a schedule that could be resolved: {entry}.");
            return;
        }

        GameLocation? claimedMap = Game1.getLocationFromName(npc.currentLocation.NameOrUniqueName);
        if (claimedMap is not null)
        {
            if (!ReferenceEquals(claimedMap, npc.currentLocation))
            {
                ModMonitor.Log($"What the hell? NPC claims to be on map {claimedMap.NameOrUniqueName} but is on the map {npc.currentLocation.NameOrUniqueName}, which has failed a reference equality check.");
            }

            if (claimedMap.characters.Contains(npc))
            {
                ModMonitor.Log($"{npc.Name} correctly in the characters list of {claimedMap}");
            }
        }

        if (schedule.StartsWith("0 "))
        {
            ModMonitor.Log($"Appears to have a zero schedule: {schedule}.");
            string locstring = schedule.GetNthChunk(' ', 1).ToString();

            if (npc.currentLocation.NameOrUniqueName != locstring)
            {

                ModMonitor.Log($"Performing hard warp");
                StreamSplit target = schedule.GetNthChunk('/').StreamSplit();
                _ = target.MoveNext(); // time

                if (!target.MoveNext())
                {
                    ModMonitor.Log($"Location could not be parsed from schedule {schedule}", LogLevel.Warn);
                    return;
                }

                GameLocation? loc = Game1.getLocationFromName(target.Current.ToString());
                if (loc is null)
                {
                    ModMonitor.Log($"Location could not be found from schedule {schedule}", LogLevel.Warn);
                    return;
                }

                if (!target.MoveNext() || !int.TryParse(target.Current, out int x))
                {
                    ModMonitor.Log($"X coords could not be parsed from schedule {schedule}", LogLevel.Warn);
                    return;
                }

                if (!target.MoveNext() || !int.TryParse(target.Current, out int y))
                {
                    ModMonitor.Log($"Y coords could not be parsed from schedule {schedule}", LogLevel.Warn);
                    return;
                }

                Game1.warpCharacter(npc, loc.NameOrUniqueName, new Point(x, y));
            }
        }
        else
        {
            ModMonitor.Log($"Does not have 0 schedule");
            if (npc.currentLocation.NameOrUniqueName != npc.DefaultMap)
            {
                ModMonitor.Log($"Seems to be on the wrong map?");
                Game1.warpCharacter(npc, npc.DefaultMap, new Point((int)npc.DefaultPosition.X / Game1.tileSize, (int)npc.DefaultPosition.Y / Game1.tileSize));
            }
        }
    }
}