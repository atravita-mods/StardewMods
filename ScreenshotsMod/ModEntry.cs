#define TRACELOG

namespace ScreenshotsMod;

using System;

using AtraCore.Framework.Internal;

using AtraShared.Utils.Extensions;

using ScreenshotsMod.Framework;
using ScreenshotsMod.Framework.ModModels;
using ScreenshotsMod.Framework.Screenshotter;
using ScreenshotsMod.Framework.UserModels;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

using AtraUtils = AtraShared.Utils.Utils;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <summary>
    /// The current live screenshotters.
    /// </summary>
    private readonly PerScreen<AbstractScreenshotter?> screenshotters = new(() => null);

    private readonly Dictionary<string, List<ProcessedRule>> _rules = [];

    private readonly List<ProcessedRule> _allMaps = [];

    /// <summary>Gets the config class for this mod.</summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        base.Entry(helper);
        I18n.Init(this.Helper.Translation);

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);
        helper.Events.Player.Warped += this.OnWarp;
        helper.Events.GameLoop.DayStarted += this.OnDayStart;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        AbstractScreenshotter.Init();

        this.Process(Config);
        helper.Events.GameLoop.DayEnding += this.Reset;
    }

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
        PackedDay today = new();
        foreach (ProcessedRule rule in this.GetCurrentValidRules(location))
        {
            if (rule.Trigger(location, today, Game1.timeOfDay))
            {
                this.TakeScreenshotImpl(location, rule.Name, rule.Path, rule.Scale, rule.DuringEvents);
                break;
            }
        }
    }

    private IEnumerable<ProcessedRule> GetCurrentValidRules(GameLocation location)
    {
        foreach (var r in this._allMaps)
        {
            yield return r;
        }

        if (this._rules.TryGetValue(location.Name, out List<ProcessedRule>? rules))
        {
            foreach (ProcessedRule i in rules)
            {
                yield return i;
            }
        }
    }

    private void TakeScreenshotImpl(GameLocation location, string name, string filename, float scale, bool duringEvent)
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

        this.Monitor.VerboseLog($"Taking screenshot for {location.NameOrUniqueName} using scale {scale}: {filename}");

        CompleteScreenshotter completeScreenshotter = new(
            Game1.player,
            this.Helper.Events.GameLoop,
            name,
            filename,
            scale,
            duringEvent,
            location);
        if (!completeScreenshotter.IsDisposed)
        {
            completeScreenshotter.Tick();
        }
        if (!completeScreenshotter.IsDisposed)
        {
            this.screenshotters.Value = completeScreenshotter;
        }
    }

    private void Process(ModConfig config)
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

                TimeRange[] times = this.FoldTimes(proposedTrigger.Time);

                ProcessedTrigger newTrigger = new(packed.Value, proposedTrigger.Time, proposedTrigger.Weather);
                processedTriggers.Add(newTrigger);

                this.Monitor.DebugOnlyLog($"Added trigger {newTrigger}");
            }

            if (processedTriggers.Count == 0)
            {
                this.Monitor.Log($"Rule {name} has no valid triggers.", LogLevel.Warn);
                continue;
            }

            ProcessedRule newRule = new(name, rule.Path, rule.Scale, rule.DuringEvents, [.. processedTriggers]);
            foreach (string map in rule.Maps)
            {
                var m = map.Trim();
                if (m == "*")
                {
                    this._allMaps.Add(newRule);
                    continue;
                }

                if (!this._rules.TryGetValue(m, out List<ProcessedRule>? prev))
                {
                    this._rules[m] = prev = [];
                }
                prev.Add(newRule);
            }
        }
    }

    private TimeRange[] FoldTimes(TimeRange[] time)
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
        return [.. proposed];
    }

    private void Reset(object? sender, DayEndingEventArgs e)
    {
        foreach (var r in this._allMaps)
        {
            r.Reset();
        }

        foreach (List<ProcessedRule> series in this._rules.Values)
        {
            foreach (ProcessedRule rule in series)
            {
                rule.Reset();
            }
        }
    }
}
