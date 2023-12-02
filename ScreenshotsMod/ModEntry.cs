// #define TRACELOG

namespace ScreenshotsMod;

using System;

using AtraCore.Framework.Internal;

using AtraShared.Integrations;
using AtraShared.Utils.Extensions;

using Newtonsoft.Json;

using ScreenshotsMod.Framework;
using ScreenshotsMod.Framework.ModModels;
using ScreenshotsMod.Framework.Screenshotter;
using ScreenshotsMod.Framework.UserModels;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

using StardewValley.Locations;

using AtraUtils = AtraShared.Utils.Utils;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    internal const string ModDataKey = "atravita.ScreenShots";

    /// <summary>
    /// The current live screenshotters.
    /// </summary>
    private readonly PerScreen<AbstractScreenshotter?> screenshotters = new(() => null);

    private readonly Dictionary<string, List<ProcessedRule>> _rules = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<ProcessedRule> _allMaps = [];

    /// <summary>Gets the config class for this mod.</summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        base.Entry(helper);
        I18n.Init(this.Helper.Translation);

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);
        this.Parse(Config);

        helper.Events.GameLoop.GameLaunched += this.RegisterGMCM;

        helper.Events.Player.Warped += this.OnWarp;
        helper.Events.GameLoop.DayStarted += this.OnDayStart;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        AbstractScreenshotter.Init();
    }

    private void RegisterGMCM(object? sender, GameLaunchedEventArgs e)
    {
        var helper = new GMCMHelper(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (!helper.TryGetAPI())
        {
            return;
        }

        helper.Register(
            reset: Config.Reset,
            save: () =>
            {
                this.Helper.AsyncWriteConfig(this.Monitor, Config);
                this.Parse(Config);
            })
            .GenerateDefaultGMCM(static () => Config);
    }

    #region triggers

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null || !Config.KeyBind.JustPressed() || Game1.game1.takingMapScreenshot)
        {
            return;
        }

        this.TakeScreenshotImpl(Game1.currentLocation, "keybind", Config.KeyBindFileName, Config.KeyBindScale, true);
    }

    private void OnDayStart(object? sender, DayStartedEventArgs e)
    {
        if (this.screenshotters.Value is { } prev)
        {
            if (prev.IsDisposed)
            {
                this.screenshotters.Value = null;
            }
        }

        if (Game1.currentLocation is null || Game1.game1.takingMapScreenshot || (Game1.currentLocation.IsTemporary && Game1.CurrentEvent?.isFestival != true))
        {
            return;
        }

        this.ProcessRules(Game1.currentLocation);
    }

    private void OnWarp(object? sender, WarpedEventArgs e)
    {
        if (this.screenshotters.Value is { } prev)
        {
            if (prev.IsDisposed)
            {
                this.screenshotters.Value = null;
            }
        }

        // it's possible for this event to be raised for a "false warp".
        if (e.NewLocation is null || ReferenceEquals(e.NewLocation, e.OldLocation) || !e.IsLocalPlayer
            || (e.NewLocation.IsTemporary && Game1.CurrentEvent?.isFestival != true) || Game1.game1.takingMapScreenshot)
        {
            return;
        }

        this.ProcessRules(e.NewLocation);
    }

    private void ProcessRules(GameLocation location)
    {
        int? lastTriggerDay = location.modData.GetInt(ModDataKey);
        uint daysSinceTriggered = uint.MaxValue;
        if (lastTriggerDay.HasValue)
        {
            var daysPlayed = Game1.stats.DaysPlayed;
            uint uLastTriggerDay;

            unchecked
            {
                uLastTriggerDay = (uint)lastTriggerDay.Value;
            }

            if (daysPlayed < uLastTriggerDay)
            {
                this.Monitor.Log($"The last trigger day is in the future, time travel must have happened, clearing data.", LogLevel.Info);
                location.modData.Remove(ModDataKey);
            }
            else
            {
                daysSinceTriggered = daysPlayed - uLastTriggerDay;
            }
        }

        if (daysSinceTriggered == 0u)
        {
            ModMonitor.DebugOnlyLog($"Map {location.NameOrUniqueName} has already had a screenshot today, skipping.");
            return;
        }

        PackedDay today = new();
        foreach (ProcessedRule rule in this.GetCurrentValidRules(location))
        {
            if (rule.CanTrigger(location, Game1.player, today, Game1.timeOfDay, daysSinceTriggered))
            {
                this.TakeScreenshotImpl(location, rule.Name, rule.Path, rule.Scale, rule.DuringEvents);
                break;
            }
        }
    }

    private IEnumerable<ProcessedRule> GetCurrentValidRules(GameLocation location)
    {
        string locationName = location switch
        {
            MineShaft shaft => shaft.getMineArea() switch
            {
                MineShaft.desertArea => "SkullCavern",
                MineShaft.quarryMineShaft => "Quarry",
                _ => "Mines"
            },
            VolcanoDungeon => "Volcano",
            _ => location.Name,
        };

        if (this._rules.TryGetValue(locationName, out List<ProcessedRule>? rules))
        {
            foreach (ProcessedRule i in rules)
            {
                yield return i;
            }
        }

        if (location is MineShaft or VolcanoDungeon)
        {
            yield break;
        }

        foreach (ProcessedRule r in this._allMaps)
        {
            yield return r;
        }

        var context = location.GetLocationContextId();
        if (!context.Equals(locationName, StringComparison.OrdinalIgnoreCase))
        {
            if (this._rules.TryGetValue(context, out List<ProcessedRule>? contextRules))
            {
                foreach (ProcessedRule i in contextRules)
                {
                    yield return i;
                }
            }
        }
    }

    private void TakeScreenshotImpl(GameLocation location, string name, string tokenizedFilename, float scale, bool duringEvent)
    {
        if (this.screenshotters.Value is { } prev)
        {
            if (prev.IsDisposed)
            {
                this.screenshotters.Value = null;
            }
            else
            {
                this.Monitor.Log($"Previous screenshot is still in effect.", LogLevel.Warn);
                return;
            }
        }

        location.modData.SetInt(ModDataKey, (int)Game1.stats.DaysPlayed);

        CompleteScreenshotter completeScreenshotter = new(
            Game1.player,
            this.Helper.Events.GameLoop,
            name,
            tokenizedFilename,
            scale,
            duringEvent,
            location);
        if (!completeScreenshotter.IsDisposed)
        {
            this.screenshotters.Value = completeScreenshotter;
        }
    }

    #endregion

    #region parsing

    private void Parse(ModConfig config)
    {
        this._rules.Clear();
        this._allMaps.Clear();

        foreach ((string name, UserRule rule) in config.Rules)
        {
            if (rule.Triggers.Length == 0)
            {
                this.Monitor.Log($"Rule {name} appears to lack triggers, skipping.", LogLevel.Warn);
                continue;
            }

            List<ProcessedTrigger> processedTriggers = new(rule.Triggers.Length);
            foreach (UserTrigger proposedTrigger in rule.Triggers)
            {
                PackedDay? packed = PackedDay.Parse(proposedTrigger.Seasons, proposedTrigger.Days, out string? error);
                if (packed is null)
                {
                    this.Monitor.Log($"Rule {name} has invalid times: {error}, skipping", LogLevel.Warn);
                    continue;
                }

                if (proposedTrigger.Time.Length == 0)
                {
                    this.Monitor.Log($"Rule {name} has no valid times, skipping.", LogLevel.Warn);
                    continue;
                }

                TimeRange[] times = FoldTimes(proposedTrigger.Time);

                processedTriggers.Add(new ProcessedTrigger(packed.Value, times, proposedTrigger.Weather, proposedTrigger.Cooldown, proposedTrigger.Condition));
            }

            if (processedTriggers.Count == 0)
            {
                this.Monitor.Log($"Rule {name} has no valid triggers.", LogLevel.Warn);
                continue;
            }

            ProcessedRule newRule = new(name, rule.Path, rule.Scale, rule.DuringEvents, processedTriggers.ToArray());

            if (rule.Maps.Length == 0)
            {
                this.Monitor.Log($"Rule {name} has no valid maps.", LogLevel.Warn);
                continue;
            }

            this.AddRuleToMap(rule.Maps[0], newRule);

            for (int m = 1; m < rule.Maps.Length; m++)
            {
                this.AddRuleToMap(rule.Maps[m], newRule.Clone());
            }
        }
    }

    private void AddRuleToMap(string mapName, ProcessedRule rule)
    {
        mapName = mapName.Trim();
        if (mapName == "*")
        {
            this._allMaps.Add(rule);
            return;
        }

        if (!this._rules.TryGetValue(mapName, out List<ProcessedRule>? prev))
        {
            this._rules[mapName] = prev = [];
        }
        prev.Add(rule);

        if (this.Monitor.IsVerbose)
        {
            this.Monitor.Log($"New rule added for {mapName}:\n\n{JsonConvert.SerializeObject(rule, Formatting.Indented)}.");
        }
    }

    private static TimeRange[] FoldTimes(TimeRange[] time)
    {
        if (time.Length is 0 or 1)
        {
            return time;
        }

        Array.Sort(time);

        bool nonoverlap = true;
        for (int i = 1; i < time.Length; i++)
        {
            TimeRange first = time[i - 1];
            TimeRange second = time[i];
            if (first.EndTime >= second.StartTime)
            {
                nonoverlap = false;
                break;
            }
        }

        if (nonoverlap)
        {
            return time;
        }

        List<TimeRange>? proposed = [];
        TimeRange prev = time[0];
        for (int i = 1; i < time.Length; i++)
        {
            TimeRange current = time[i];
            if (current.StartTime <= prev.EndTime)
            {
                prev = new(prev.StartTime, Math.Max(prev.EndTime, current.EndTime));
            }
            else
            {
                proposed.Add(prev);
                prev = current;
            }
        }
        proposed.Add(prev);
        return proposed.ToArray();
    }

    #endregion
}
