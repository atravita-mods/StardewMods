#if DEBUG
using System.Diagnostics;
#endif

using AtraBase.Toolkit;

using AtraCore.Config;
using AtraCore.Framework.DialogueManagement;
using AtraCore.Framework.Internal;
using AtraCore.Framework.ItemManagement;
using AtraCore.Framework.QueuePlayerAlert;
using AtraCore.HarmonyPatches.DrawPrismaticPatches;
using AtraCore.Utilities;

using AtraShared.ConstantsAndEnums;
using AtraShared.MigrationManager;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewModdingAPI.Events;

using AtraUtils = AtraShared.Utils.Utils;

namespace AtraCore;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private MigrationManager? migrator;

    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <summary>
    /// Gets the config for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModMonitor = this.Monitor;
        AssetManager.Initialize(helper.GameContent);

        // replace AtraBase's logger with SMAPI's logging service.
        AtraBase.Internal.Logger.Instance = new Logger(this.Monitor);

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.Events.Content.AssetRequested += this.OnAssetRequested;

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.DayEnding += this.OnDayEnd;
        helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;

#if DEBUG
        helper.Events.GameLoop.DayStarted += this.OnDayStart;
        helper.Events.GameLoop.SaveLoaded += this.LateSaveLoaded;
#endif
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // initialize data caches
        DataToItemMap.Init(this.Helper.GameContent);
        this.Helper.Events.Content.AssetsInvalidated += this.OnAssetInvalidation;

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsSplitScreen && Context.ScreenId != 0)
        {
            return;
        }

        Game1.player.team.completedSpecialOrders.OnValueAdded += this.OnValueAdded;

        MultiplayerHelpers.AssertMultiplayerVersions(this.Helper.Multiplayer, this.ModManifest, this.Monitor, this.Helper.Translation);
        DrawPrismatic.LoadPrismaticData();

        this.migrator = new(this.ModManifest, this.Helper, this.Monitor);
        if (!this.migrator.CheckVersionInfo())
        {
            this.Helper.Events.GameLoop.Saved += this.WriteMigrationData;
        }
        else
        {
            this.migrator = null;
        }
    }

    private void OnValueAdded(string key, bool value) => this.Monitor.Log(key, LogLevel.Alert);

    /// <inheritdoc cref="IGameLoopEvents.TimeChanged"/>
    /// <remarks>Currently handles: pushing delayed dialogue back onto the stack, and player alerts.</remarks>
    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        QueuedDialogueManager.PushPossibleDelayedDialogues();
        PlayerAlertHandler.DisplayFromQueue();
    }

    private void OnDayEnd(object? sender, DayEndingEventArgs e)
        => QueuedDialogueManager.ClearDelayedDialogue();

    #region assets

    private void OnAssetInvalidation(object? sender, AssetsInvalidatedEventArgs e)
        => DataToItemMap.Reset(e.NamesWithoutLocale);

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        => AssetManager.Apply(e);

    #endregion

    private void ApplyPatches(Harmony harmony)
    {
#if DEBUG
        Stopwatch sw = new();
        sw.Start();
#endif
        try
        {
            harmony.PatchAll(typeof(ModEntry).Assembly);
        }
        catch (Exception ex)
        {
            this.Monitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, uniqueID: harmony.Id, transpilersOnly: true);
#if DEBUG
        sw.Stop();
        this.Monitor.Log($"Took {sw.ElapsedMilliseconds} ms to apply harmony patches.", LogLevel.Info);
#endif
    }


    /// <inheritdoc cref="IGameLoopEvents.Saved"/>
    /// <remarks>
    /// Writes migration data then detaches the migrator.
    /// </remarks>
    private void WriteMigrationData(object? sender, SavedEventArgs e)
    {
        if (this.migrator is not null)
        {
            this.migrator.SaveVersionInfo();
            this.migrator = null;
        }
        this.Helper.Events.GameLoop.Saved -= this.WriteMigrationData;
    }

#if DEBUG
    [EventPriority(EventPriority.Low - 1000)]
    private void OnDayStart(object? sender, DayStartedEventArgs e)
    {
        if (Context.IsSplitScreen && Context.ScreenId != 0)
        {
            return;
        }

        this.Monitor.DebugOnlyLog($"Current memory usage {GC.GetTotalMemory(false):N0}", LogLevel.Info);
        GC.Collect();
        this.Monitor.DebugOnlyLog($"Post-collection memory usage {GC.GetTotalMemory(true):N0}", LogLevel.Info);
    }
#endif

    [EventPriority(EventPriority.Low - 1000)]
    private void LateSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsSplitScreen && Context.ScreenId != 0)
        {
            return;
        }
        this.Monitor.DebugOnlyLog($"Current memory usage {GC.GetTotalMemory(false):N0}", LogLevel.Info);
        GCHelperFunctions.RequestFullGC();
        this.Monitor.DebugOnlyLog($"Post-collection memory usage {GC.GetTotalMemory(true):N0}", LogLevel.Info);
    }
}
