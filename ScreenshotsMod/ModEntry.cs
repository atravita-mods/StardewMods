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
    /// <summary>
    /// The mod data key used to mark maps.
    /// </summary>
    internal const string ModDataKey = "atravita.ScreenShots";

    /// <summary>
    /// The maximum number of screenshots we allow to be active at any one time.
    /// </summary>
    private const int MAX_ACTIVE_SCREENSHOTS = 8; // I goddamn hope you don't hit this.

    /// <summary>
    /// The current live screenshotters.
    /// </summary>
    private readonly List<AbstractScreenshotter> screenshotters = [];
    private readonly PerScreen<int> lastPressedTick = new(static () => -1);
    private readonly PerScreen<bool> modTriggeredWarp = new(() => false);

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

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunch;

        helper.Events.Player.Warped += this.OnWarp;
        helper.Events.GameLoop.DayStarted += this.OnDayStart;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.GameLoop.SaveLoaded += this.ValidateMapRules;
        AbstractScreenshotter.Init();
    }

    private void OnGameLaunch(object? sender, GameLaunchedEventArgs e)
    {
        this.Parse(Config);

        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
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

        int ticks = Game1.ticks;
        if (this.lastPressedTick.Value + 60 > ticks || !this.CheckScreenshotCapacity())
        {
            Game1.showRedMessage(I18n.SlowDown());
            return;
        }
        this.lastPressedTick.Value = ticks;

        this.TakeScreenshotImpl(Game1.currentLocation, "keybind", Config.KeyBindFileName, Config.KeyBindScale, true);
    }

    [EventPriority(EventPriority.Low - 1000)] // use a low event priority so we're "closer" to our next tick.
    private void OnDayStart(object? sender, DayStartedEventArgs e)
    {
        if (Game1.currentLocation is null || Game1.game1.takingMapScreenshot || (Game1.currentLocation.IsTemporary && Game1.CurrentEvent?.isFestival != true))
        {
            return;
        }

        this.ProcessRules(Game1.currentLocation);
    }

    [EventPriority(EventPriority.Low - 1000)]
    private void OnWarp(object? sender, WarpedEventArgs e)
    {
        // it's possible for this event to be raised for a "false warp".
        if (e.NewLocation is null || ReferenceEquals(e.NewLocation, e.OldLocation) || !e.IsLocalPlayer
            || (e.NewLocation.IsTemporary && Game1.CurrentEvent?.isFestival != true) || Game1.game1.takingMapScreenshot)
        {
            return;
        }

        if (this.modTriggeredWarp.Value)
        {
            this.modTriggeredWarp.Value = false;
            return;
        }

        this.ProcessRules(e.NewLocation);
    }

    private void ProcessRules(GameLocation location)
    {
        if (!this.CheckScreenshotCapacity())
        {
            this.Monitor.Log($"Too many active screenshotters. This is likely an error.", LogLevel.Error);
            return;
        }

        uint daysSinceTriggered = GetDaysSinceLastTriggered(location);

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
                if (!rule.DuringEvents && Game1.CurrentEvent is { } evt)
                {
                    // we may have to warp players BACK to the original map after an event.
                    // this lets us delay a screenshot to after an event.
                    evt.onEventFinished += () =>
                    {
                        if (Game1.currentLocation.NameOrUniqueName != location.NameOrUniqueName)
                        {
                            this.Monitor.Log($"Warping farmer back to original map after event {evt.id} for screenshots.");
                            LocationRequest locationRequest = new(location.NameOrUniqueName, location.isStructure.Value, location);
                            locationRequest.OnLoad += () =>
                            {
                                this.TakeScreenshotImpl(location, rule.Name, rule.Path, rule.Scale, rule.DuringEvents);
                                this.modTriggeredWarp.Value = false;
                            };
                            int x = 8;
                            int y = 8;
                            Utility.getDefaultWarpLocation(location.Name, ref x, ref y);
                            this.modTriggeredWarp.Value = true;
                            Game1.warpFarmer(locationRequest, x, y, 2);
                        }
                        else
                        {
                            this.Monitor.Log($"Taking event-delayed screenshot after {evt.id} for {rule.Name}.");
                            this.TakeScreenshotImpl(location, rule.Name, rule.Path, rule.Scale, rule.DuringEvents);
                        }
                    };
                }
                else
                {
                    this.TakeScreenshotImpl(location, rule.Name, rule.Path, rule.Scale, rule.DuringEvents);
                }
                break;
            }
        }
    }

    private static uint GetDaysSinceLastTriggered(GameLocation location)
    {
        int? lastTriggerDay = location.modData.GetInt(ModDataKey);
        uint daysSinceTriggered = uint.MaxValue;
        if (lastTriggerDay.HasValue)
        {
            uint daysPlayed = Game1.stats.DaysPlayed;
            uint uLastTriggerDay;

            unchecked
            {
                uLastTriggerDay = (uint)lastTriggerDay.Value;
            }

            if (daysPlayed < uLastTriggerDay)
            {
                ModMonitor.Log($"The last trigger day is in the future, time travel must have happened, clearing data.", LogLevel.Info);
                location.modData.Remove(ModDataKey);
            }
            else
            {
                daysSinceTriggered = daysPlayed - uLastTriggerDay;
            }
        }

        return daysSinceTriggered;
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

        string context = location.GetLocationContextId();
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
            this.screenshotters.Add(completeScreenshotter);
        }
    }

    private bool CheckScreenshotCapacity()
    {
        int count = this.screenshotters.Count;
        if (count == 0)
        {
            return true;
        }

        this.screenshotters.RemoveAll(static shot => shot.IsDisposed);
        return this.screenshotters.Count < MAX_ACTIVE_SCREENSHOTS;
    }

    #endregion

    #region parsing

    private void Parse(ModConfig config)
    {
        this._rules.Clear();
        this._allMaps.Clear();

        foreach ((string name, UserRule rule) in config.Rules)
        {
            if (rule.Process(name) is { } newRule)
            {
                foreach (string map in rule.Maps)
                {
                    this.AddRuleToMap(map, newRule);
                }
            }
        }
    }

    private void AddRuleToMap(string mapName, ProcessedRule rule)
    {
        if (this.Monitor.IsVerbose)
        {
            this.Monitor.Log($"New rule added for {mapName}:\n\n{JsonConvert.SerializeObject(rule, Formatting.Indented)}.");
        }

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
    }

    // we must wait until the save is loaded for maps to be loaded. This just
    // gives the rules a once-over to double check they're usable, and warns if not.
    private void ValidateMapRules(object? sender, SaveLoadedEventArgs e)
    {
        foreach (string mapName in this._rules.Keys)
        {
            // check if rule is valid.
            if (mapName is not "Mines" and not "Quarry" and not "SkullCavern" and not "Volcano"
                && Game1.getLocationFromName(mapName) is null && !Game1.locationContextData.ContainsKey(mapName))
            {
                this.Monitor.Log($"Map {mapName} may correspond to a location that does not exist.", LogLevel.Warn);
            }
        }
    }

    #endregion
}
