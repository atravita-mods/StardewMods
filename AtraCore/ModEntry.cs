
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
using AtraCore.Framework.GameStateQueries;
using AtraCore.Framework.Internal;
using AtraCore.Framework.ItemManagement;
using AtraCore.Framework.ItemResolvers;
using AtraCore.Framework.QueuePlayerAlert;
using AtraCore.Framework.TriggerActions;
using AtraCore.HarmonyPatches;
using AtraCore.HarmonyPatches.CustomEquipPatches;
using AtraCore.HarmonyPatches.DrawPrismaticPatches;
using AtraCore.Utilities;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.MigrationManager;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewModdingAPI.Events;

using StardewValley.Delegates;
using StardewValley.Internal;
using StardewValley.Triggers;

using AtraUtils = AtraShared.Utils.Utils;

namespace AtraCore;

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
        // apply harmony.
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

        // initialize data caches
        DataToItemMap.Init(this.Helper.GameContent);
        this.Helper.Events.Content.AssetsInvalidated += this.OnAssetInvalidation;

        // add event commands and preconditions.
        Event.RegisterPrecondition("atravita_PlayerRelationship", PlayerRelationshipPreconditions.PlayerRelationshipStatus);
        Event.RegisterCommand("atravita_" + nameof(FacePlayer), FacePlayer.FacePlayerCommand);
        Event.RegisterCommand("atravita_" + nameof(AllowRepeatAfter), AllowRepeatAfter.SetRepeatAfter);
        Event.RegisterCommand("atravita_" + nameof(BranchIf), BranchIf.BranchIfCommand);
        Event.RegisterCommand("atravita_" + nameof(GainExperience), GainExperience.Command);

        SetInvisible invisible = new (this.Helper.Multiplayer, this.ModManifest.UniqueID);
        Event.RegisterCommand("atravita_" + nameof(SetInvisible), invisible.ApplyInvisibility);
        MultiplayerDispatch.Register(SetInvisible.RequestSetInvisible, invisible.ProcessSetInvisibleRequest);

        SetRelationship setrelationship = new(this.Helper.Multiplayer, this.ModManifest.UniqueID);
        Event.RegisterCommand("atravita_" + nameof(SetRelationship), setrelationship.ApplySetRelationship);
        MultiplayerDispatch.Register(SetRelationship.RequestNPCMove, SetRelationship.ProcessMoveRequest);

        // actions
        GameLocation.RegisterTileAction("atravita.AtraCore_Teleport", TeleportPlayer.ApplyCommand);

        ItemQueryResolver.Register("atravita.AtraCore_CHOOSE_K", ChooseKQuery.ChooseK);
        ItemQueryResolver.Register("atravita.AtraCore_GENERIC_FLAVORED_ITEM", GenericFlavoredItemQuery.Generate);
        ItemQueryResolver.Register("atravita_raccoon_seed", RaccoonSeedQuery.Query);
        ItemQueryResolver.Register("atravita_missing_artifact_or", MissingArtifact.Query);
        ItemQueryResolver.Register("atravita_shop_forward", ShopForwardQuery.Query);

        AddGSQ("atravita.AtraCore_HAS_EARNED_MONEY", MoneyEarned.CheckMoneyEarned);
        AddGSQ("atravita.AtraCore_HAS_DAILY_LUCK", CurrentDailyLuck.DailyLuck);
        AddGSQ("atravita.AtraCore_RECIPES_COOKED_PERCENT", RecipesCooked.RecipesCookedPercent);
        AddGSQ("atravita.AtraCore_FISH_CAUGHT_PERCENT", FishCaught.FishCaughtPercent);
        AddGSQ("atravita.AtraCore_NPC_WAS_GIFTED", NPCWasGifted.Query);

        // various NPC queries
        AddGSQ("atravita.AtraCore_NPC_WAS_TALKED_TO", NPCQueries.WasTalkedTo);
        AddGSQ("atravita.AtraCore_NPC_WAS_LAST_GIFTED", NPCQueries.WasLastGifted);
        AddGSQ("atravita.AtraCore_NPC_DAYS_SINCE_MARRIAGE", NPCQueries.DaysSinceMarriage);
        AddGSQ("atravita.AtraCore_NPC_IS_ANNIVERSARY", NPCQueries.IsAnniversaryOfMarriage);

        AddGSQ("atravita.AtraCore_IS_SEED_GROWING_IN", IsSeedGrowingIn.SeedQuery);
        AddGSQ("atravita.AtraCore_IS_SAPLING_GROWING_IN", IsSeedGrowingIn.SaplingQuery);

        AddGSQ("atravita.AtraCore_FARMER_IS_WEARING", FarmerQueries.IsWearing);

        AddGSQ("atravita.AtraCore_IS_ERROR_ITEM", ItemQueries.ErrorItem);

        void AddGSQ(string query, GameStateQueryDelegate del)
        {
            if (GameStateQuery.Exists(query))
            {
                this.Monitor.Log($"{query} seems to exist already as a GSQ, what.", LogLevel.Warn);
            }
            else
            {
                GameStateQuery.Register(query, del);
            }
        }

        // add trigger actions
        TriggerActionManager.RegisterAction("atravita.AtraCore_EMOTE", NPCActions.Emote);
        TriggerActionManager.RegisterAction("atravita.AtraCore_NPC_TEXT_OVER_HEAD", NPCActions.TextOverHead);
        TriggerActionManager.RegisterAction("atravita.AtraCore_JUMP", NPCActions.Jump);
        TriggerActionManager.RegisterAction("atravita.AtraCore_FACE_DIRECTION", NPCActions.FaceDirection);
        TriggerActionManager.RegisterAction("atravita.AtraCore_OPEN_SHOP", NPCActions.OpenShop);
        TriggerActionManager.RegisterAction("atravita.AtraCore_CHANGE_APPEARANCE", NPCActions.ChangeAppearance);

        // integrations
        {
            IntegrationHelper integration = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry);
            if (integration.TryGetAPI("sinZandAtravita.SinZsEventTester", "0.1.2", out IEventTesterAPI? eventTester))
            {
                eventTester.RegisterAsset(this.Helper.GameContent.ParseAssetName(AtraCoreConstants.EquipData));
            }
        }
    }

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        MissingArtifact.Reset();
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

        ItemPatcher.OnReturnToTitle();
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
