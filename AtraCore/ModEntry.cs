namespace AtraCore;

#if DEBUG
using System.Diagnostics;
#endif

using AtraBase.Toolkit;

using AtraCore.Config;
using AtraCore.Framework;
using AtraCore.Framework.ActionCommands;
using AtraCore.Framework.Caches;
using AtraCore.Framework.ConsoleCommands;
using AtraCore.Framework.DialogueManagement;
using AtraCore.Framework.EventCommands;
using AtraCore.Framework.EventCommands.AllowRepeatCommand;
using AtraCore.Framework.EventCommands.RelationshipCommands;
using AtraCore.Framework.EventPreconditions;
using AtraCore.Framework.Internal;
using AtraCore.Framework.ItemManagement;
using AtraCore.Framework.QueuePlayerAlert;
using AtraCore.HarmonyPatches;
using AtraCore.HarmonyPatches.CustomEquipPatches;
using AtraCore.HarmonyPatches.DrawPrismaticPatches;
using AtraCore.Utilities;

using AtraShared.ConstantsAndEnums;
using AtraShared.MigrationManager;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewModdingAPI.Events;

using AtraUtils = AtraShared.Utils.Utils;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    private MigrationManager? migrator;

    /// <summary>
    /// Gets the config for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        base.Entry(helper);

        I18n.Init(helper.Translation);
        AssetManager.Initialize(helper.GameContent);
        QuestTracker.Initialize(helper.Multiplayer, this.ModManifest.UniqueID);

        MultiplayerDispatch.Initialize(this.ModManifest.UniqueID);
        helper.Events.Multiplayer.ModMessageReceived += static (_, e) => MultiplayerDispatch.Apply(e);

        // replace AtraBase's logger with SMAPI's logging service.
        AtraBase.Internal.Logger.Instance = new Logger(this.Monitor);

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.Content.LocaleChanged += static (_, _) => AssetManager.OnLocaleChange();

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.Saving += (_, _) => QuestTracker.Write(this.Helper.Data);

        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;

        helper.Events.GameLoop.DayEnding += this.OnDayEnd;
        helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        helper.Events.Player.Warped += this.Player_Warped;
        helper.Events.GameLoop.UpdateTicked += static (_, e) => ItemPatcher.UpdateEquips(e);
        helper.Events.GameLoop.DayStarted += static (_, _) => ItemPatcher.OnDayStart();

        helper.Events.Multiplayer.PeerConnected += this.Multiplayer_PeerConnected;
        helper.Events.Multiplayer.ModMessageReceived += this.Multiplayer_ModMessageReceived;

        EventCommandManager.Add(new RemoveMail("atravita_" + nameof(RemoveMail), this.Monitor));
        EventCommandManager.Add(new AllowRepeatAfter("atravita_" + nameof(AllowRepeatAfter), this.Monitor));

        SetRelationship setrelationship = new("atravita_" + nameof(SetRelationship), this.Monitor, this.Helper.Multiplayer, this.ModManifest.UniqueID);
        EventCommandManager.Add(setrelationship);
        helper.Events.Multiplayer.ModMessageReceived += setrelationship.ProcessMoveRequest;

        SetInvisible invisible = new("atravita_" + nameof(SetInvisible), this.Monitor, this.Helper.Multiplayer, this.ModManifest.UniqueID);
        EventCommandManager.Add(invisible);
        helper.Events.Multiplayer.ModMessageReceived += invisible.ProcessSetInvisibleRequest;

        CommandManager.Register(helper.ConsoleCommands);

#if DEBUG
        if (!helper.ModRegistry.IsLoaded("DigitalCarbide.SpriteMaster"))
        {
            helper.Events.GameLoop.DayStarted += this.OnDayStart;
            helper.Events.GameLoop.SaveLoaded += this.LateSaveLoaded;
        }
#endif
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // initialize data caches
        DataToItemMap.Init(this.Helper.GameContent);
        this.Helper.Events.Content.AssetsInvalidated += this.OnAssetInvalidation;

        // add event commands and preconditions.
        Event.RegisterCustomPrecondition("atravita_PlayerRelationship", PlayerRelationshipPreconditions.PlayerRelationshipStatus);

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

        // actions
        GameLocation.RegisterTileAction("atravita.Teleport", TeleportPlayer.ApplyCommand);
    }

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        try
        {
            AllowRepeatAfterHandler.Load(this.Helper.Data);
        }
        catch (Exception ex)
        {
            this.Monitor.LogError("reading events to repeat file", ex);
        }

        if (Context.IsSplitScreen && Context.ScreenId != 0)
        {
            return;
        }

        MultiplayerHelpers.AssertMultiplayerVersions(this.Helper.Multiplayer, this.ModManifest, this.Monitor, this.Helper.Translation);
        DrawPrismatic.LoadPrismaticData();
        QuestTracker.Load(this.Helper.Data);

        this.Helper.GameContent.InvalidateCacheAndLocalized("Data/Objects");

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

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        NPCCache.Reset();
        AllowRepeatAfterHandler.Reset();
        QuestTracker.Reset();
    }

    /// <inheritdoc cref="IGameLoopEvents.TimeChanged"/>
    /// <remarks>Currently handles: pushing delayed dialogue back onto the stack, and player alerts.</remarks>
    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        QueuedDialogueManager.PushPossibleDelayedDialogues();
        PlayerAlertHandler.DisplayFromQueue();
    }

    /// <inheritdoc cref="IPlayerEvents.Warped"/>
    private void Player_Warped(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer)
        {
            return;
        }

        ItemPatcher.Reset();
        ItemPatcher.OnPlayerLocationChange(e);

        if (PlayerAlertHandler.HasMessages())
        {
            int count = 3 - Game1.hudMessages.Count;
            if (count > 0)
            {
                PlayerAlertHandler.DisplayFromQueue(count);
            }
        }
    }

    /// <inheritdoc cref="IGameLoopEvents.DayEnding"/>
    private void OnDayEnd(object? sender, DayEndingEventArgs e)
    {
        QueuedDialogueManager.ClearDelayedDialogue();

        AllowRepeatAfterHandler.DayEnd();
        AllowRepeatAfterHandler.Save(this.Helper.Data);
    }

    #region assets

    private void OnAssetInvalidation(object? sender, AssetsInvalidatedEventArgs e)
    {
        DataToItemMap.Reset(e.NamesWithoutLocale);
        AssetManager.Invalidate(e.NamesWithoutLocale);
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        => AssetManager.Apply(e);

    #endregion

    #region multiplayer

    private void Multiplayer_ModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        QuestTracker.OnMessageReceived(e);
    }

    private void Multiplayer_PeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        QuestTracker.OnPeerConnected(e);
    }

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
        this.Monitor.LogTimespan("Applying harmony patches", sw);
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
