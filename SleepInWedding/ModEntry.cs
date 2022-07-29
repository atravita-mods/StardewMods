using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using StardewModdingAPI.Events;

using AtraUtils = AtraShared.Utils.Utils;

namespace SleepInWedding;

/// <inheritdoc />
[HarmonyPatch(typeof(GameLocation))]
internal sealed class ModEntry : Mod
{
    private bool checkForWedding = true;

    /// <summary>
    /// Gets the config for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <summary>
    /// Gets the logging instance for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModMonitor = this.Monitor;

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoad;
        helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        helper.Events.GameLoop.DayEnding += this.OnDayEnd;

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    private void OnDayEnd(object? sender, DayEndingEventArgs e)
        => this.checkForWedding = true;

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (Game1.weddingsToday.Count > 0)
        {
            if (e.NewTime == 610)
            {
                if (Game1.player.HasWeddingToday())
                {
                    int hour = Math.DivRem(Config.WeddingTime, 100, out int minutes);
                    Game1.addHUDMessage(new HUDMessage(I18n.WeddingMessage(hour, minutes), HUDMessage.achievement_type));
                }
            }
            else if (Game1.timeOfDay == Config.WeddingTime - 10)
            {
                Game1.addHUDMessage(new HUDMessage(I18n.WeddingReminder(), HUDMessage.achievement_type));
            }
            else if (this.checkForWedding && Game1.timeOfDay >= Config.WeddingTime)
            {
                Game1.player.currentLocation.checkForEvents();
                this.checkForWedding = false;
            }
        }
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (helper.TryGetAPI())
        {
            helper.Register(
                reset: static () => Config = new(),
                save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config))
            .GenerateDefaultGMCM(static () => Config);
        }
    }

    /// <summary>
    /// calls queueWeddingsForToday just after save is loaded.
    /// Game doesn't seem to call it.
    /// </summary>
    /// <param name="sender">SMAPI.</param>
    /// <param name="e">Event args.</param>
    private void OnSaveLoad(object? sender, SaveLoadedEventArgs e)
    {
        ModMonitor.DebugOnlyLog($"Before attempting queuing new weddings {string.Join(", ", Game1.weddingsToday)}");
        ModMonitor.DebugOnlyLog($"{Game1.player.spouse ?? "null"}");
        ModMonitor.DebugOnlyLog($"{Game1.player.isEngaged()}");
        ModMonitor.DebugOnlyLog($"{Game1.player.isMarried()}");
        ModMonitor.DebugOnlyLog($"{Game1.player.friendshipData[Game1.player.spouse].CountdownToWedding}");
        ModMonitor.DebugOnlyLog($"{Game1.Date.TotalDays} {Game1.player.friendshipData[Game1.player.spouse].WeddingDate.TotalDays}");
        if (Context.IsMainPlayer)
        {
            Game1.queueWeddingsForToday();
        }
        ModMonitor.DebugOnlyLog($"Current weddings {string.Join(", ", Game1.weddingsToday)}");
    }

    /// <summary>
    /// Applies the patches for this mod.
    /// </summary>
    /// <param name="harmony">This mod's harmony instance.</param>
    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, this.ModManifest.UniqueID, transpilersOnly: true);
    }
}