﻿using System.Reflection;
using AtraBase.Toolkit.Reflection;
using AtraCore.Framework.ReflectionManager;
using AtraCore.Utilities;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.Integrations.Interfaces.ContentPatcher;
using AtraShared.MigrationManager;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using SpecialOrdersExtended.HarmonyPatches;
using SpecialOrdersExtended.Managers;
using SpecialOrdersExtended.Niceties;
using StardewModdingAPI.Events;

using StardewValley.GameData;

using AtraUtils = AtraShared.Utils.Utils;

namespace SpecialOrdersExtended;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private static readonly string[] ModsThatHandleTheBoard = new string[] { "Rafseazz.RidgesideVillage", "PurrplingCat.QuestFramework", "Esca.EMP" };
    private bool hasModsThatHandleBoard = false;

    private PlayerTeamWatcher? watcher;

    /// <summary>
    /// SpaceCore API handle.
    /// </summary>
    /// <remarks>If null, means API not loaded.</remarks>
    private static ISpaceCoreAPI? spaceCoreAPI;

    /// <summary>
    /// Gets the SpaceCore API instance.
    /// </summary>
    /// <remarks>If null, was not able to be loaded.</remarks>
    internal static ISpaceCoreAPI? SpaceCoreAPI => spaceCoreAPI;

    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <summary>
    /// Gets SMAPI's data helper for this mod.
    /// </summary>
    internal static IDataHelper DataHelper { get; private set; } = null!;

    /// <summary>
    /// Gets SMAPI's Multiplayer helper for this mod.
    /// </summary>
    internal static IMultiplayerHelper MultiplayerHelper { get; private set; } = null!;

    /// <summary>
    /// Gets the config class for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Field kept near accessors.")]
    private static readonly Lazy<Func<string, bool>> CheckTagLazy = new(
        typeof(SpecialOrder)
            .GetCachedMethod("CheckTag", ReflectionCache.FlagTypes.StaticFlags)
            .CreateDelegate<Func<string, bool>>);

    private static Func<string, bool> CheckTagDelegate => CheckTagLazy.Value;

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Reviewed.")]
    private MigrationManager? migrator;

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        // Bind useful SMAPI features.
        I18n.Init(helper.Translation);
        AssetManager.Initialize(helper.GameContent);
        CustomEmoji.Init(helper.GameContent);
        ModMonitor = this.Monitor;
        DataHelper = helper.Data;
        MultiplayerHelper = helper.Multiplayer;

        this.Monitor.Log($"Starting up: {this.ModManifest.UniqueID} - {typeof(ModEntry).Assembly.FullName}");

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        // Register console commands.
        helper.ConsoleCommands.Add(
            name: "special_order_pool",
            documentation: I18n.SpecialOrderPool_Description(),
            callback: this.GetAvailableOrders);
        helper.ConsoleCommands.Add(
            name: "check_tag",
            documentation: I18n.CheckTag_Description(),
            callback: this.ConsoleCheckTag);
        helper.ConsoleCommands.Add(
            name: "list_available_stats",
            documentation: I18n.ListAvailableStats_Description(),
            callback: StatsManager.ConsoleListProperties);
        helper.ConsoleCommands.Add(
            name: "special_orders_dialogue",
            documentation: $"{I18n.SpecialOrdersDialogue_Description()}\n\n{I18n.SpecialOrdersDialogue_Example()}\n    {I18n.SpecialOrdersDialogue_Usage()}\n    {I18n.SpecialOrdersDialogue_Save()}",
            callback: DialogueManager.ConsoleSpecialOrderDialogue);

        // Register event handlers.
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.SaveLoaded;
        helper.Events.GameLoop.Saving += this.Saving;
        helper.Events.GameLoop.DayEnding += this.OnDayEnd;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;

        helper.Events.GameLoop.TimeChanged += static (_, _) => RecentSOManager.GrabNewRecentlyCompletedOrders();
        helper.Events.GameLoop.OneSecondUpdateTicked += this.OneSecondUpdateTicked;

        helper.Events.Content.AssetRequested += static (_, e) => AssetManager.OnLoadAsset(e);
        helper.Events.Content.AssetsInvalidated += static (_, e) => AssetManager.Reset(e.NamesWithoutLocale);

        helper.Events.Content.AssetReady += static (_, e) => CustomEmoji.Ready(e);
        helper.Events.Content.AssetsInvalidated += static (_, e) => CustomEmoji.Reset(e.NamesWithoutLocale);
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.Patch(
                original: typeof(NPC).GetCachedMethod(nameof(NPC.checkForNewCurrentDialogue), ReflectionCache.FlagTypes.InstanceFlags),
                postfix: new HarmonyMethod(typeof(DialogueManager), nameof(DialogueManager.PostfixCheckDialogue)));
        }
        catch (Exception ex)
        {
            ModMonitor.Log($"Failed to patch NPC::checkForNewCurrentDialogue for Special Orders Dialogue. Dialogue will be disabled\n\n{ex}", LogLevel.Error);
        }

        if (ModsThatHandleTheBoard.All(uniqueID => !this.Helper.ModRegistry.IsLoaded(uniqueID)))
        {
            this.Monitor.Log("Applying patch to suppress board updates.");
            SpecialOrderPatches.ApplyUpdatePatch(harmony);
        }
        else
        {
            this.hasModsThatHandleBoard = true;
        }

        try
        {
            harmony.PatchAll(typeof(ModEntry).Assembly);
        }
        catch (Exception ex)
        {
            this.Monitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }

        try
        {
            DebuggingPatches.Apply(harmony);
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Failed while trying to apply debugging patches.\n\n{ex}", LogLevel.Warn);
        }

        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    /// <remarks>Used to bind APIs and register CP tokens.</remarks>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        Harmony? harmony = new(this.ModManifest.UniqueID);
        this.ApplyPatches(harmony);

        // Bind SpaceCore API
        IntegrationHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, LogLevel.Trace);
        if (helper.TryGetAPI("spacechase0.SpaceCore", "1.5.10", out spaceCoreAPI))
        {
            MethodInfo eventcommand = typeof(EventCommands).StaticMethodNamed(nameof(EventCommands.AddSpecialOrder));
            spaceCoreAPI.AddEventCommand(EventCommands.ADD_SPECIAL_ORDER, eventcommand);
        }
        else
        {
            this.Monitor.Log("SpaceCore not detected, handling event commands myself", LogLevel.Info);
            harmony.Patch(
                original: typeof(Event).GetCachedMethod(nameof(Event.tryEventCommand), ReflectionCache.FlagTypes.InstanceFlags),
                prefix: new HarmonyMethod(typeof(EventCommands), nameof(EventCommands.PrefixTryGetCommand)));
        }

        if (helper.TryGetAPI("Pathoschild.ContentPatcher", "1.20.0", out IContentPatcherAPI? api))
        {
            api.RegisterToken(this.ModManifest, "Current", new Tokens.CurrentSpecialOrders());
            api.RegisterToken(this.ModManifest, "Available", new Tokens.AvailableSpecialOrders());
            api.RegisterToken(this.ModManifest, "Completed", new Tokens.CompletedSpecialOrders());
            api.RegisterToken(this.ModManifest, "CurrentRules", new Tokens.CurrentSpecialOrderRule());
            api.RegisterToken(this.ModManifest, "RecentCompleted", new Tokens.RecentCompletedSO());

            api.RegisterToken(
                mod: this.ModManifest,
                name: "ProfitMargin",
                getValue: static () => new[] { Math.Round(Game1.MasterPlayer.difficultyModifier, 2).ToString() });
        }

        if (helper.TryGetAPI("Omegasis.SaveAnywhere", "2.13.0", out ISaveAnywhereApi? saveAnywhereApi))
        {
            saveAnywhereApi.BeforeSave += this.BeforeSaveAnywhere;
            saveAnywhereApi.AfterLoad += this.AfterSaveAnywhere;
        }

        {
            GMCMHelper gmcmHelper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
            if (gmcmHelper.TryGetAPI())
            {
                gmcmHelper.Register(
                    reset: static () => Config = new(),
                    save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config))
                .GenerateDefaultGMCM(static () => Config);
            }
        }
    }

    #region save anywhere

    private void AfterSaveAnywhere(object? sender, EventArgs e)
    {
        try
        {
            DialogueManager.LoadTemp();
            RecentSOManager.LoadTemp();
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Failed in loading temporary files:\n\n{ex}", LogLevel.Error);
        }
    }

    private void BeforeSaveAnywhere(object? sender, EventArgs e)
    {
        try
        {
            DialogueManager.SaveTemp();
            RecentSOManager.SaveTemp();
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Failed in saving temporary files:\n\n{ex}", LogLevel.Error);
        }
    }

    #endregion

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    /// <remarks>Used to load in this mod's data models.</remarks>
    private void SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.Monitor.DebugOnlyLog("Event SaveLoaded raised");
        DialogueManager.Load(Game1.player.UniqueMultiplayerID);
        MultiplayerHelpers.AssertMultiplayerVersions(this.Helper.Multiplayer, this.ModManifest, this.Monitor, this.Helper.Translation);

        if (Context.IsSplitScreen && Context.ScreenId != 0)
        {
            return;
        }
        this.migrator = new(this.ModManifest, this.Helper, this.Monitor);

        if (!this.migrator.CheckVersionInfo())
        {
            this.Helper.Events.GameLoop.Saved += this.WriteMigrationData;
        }
        else
        {
            this.migrator = null;
        }
        RecentSOManager.Load();
        this.watcher = new();
    }

    /// <inheritdoc cref="IGameLoopEvents.Saving"/>
    /// <remarks>Used to handle day-end events.</remarks>
    private void Saving(object? sender, SavingEventArgs e)
    {
        this.Monitor.DebugOnlyLog("Event Saving raised");

        TagManager.ResetRandom();
        TagManager.ClearCache();

        if (!this.hasModsThatHandleBoard && !SpecialOrder.IsSpecialOrdersBoardUnlocked())
        {
            this.Monitor.Log($"Board is not open, skipping saving");
            return;
        }

        DialogueManager.Save();

        if (Context.IsSplitScreen && Context.ScreenId != 0)
        {// Some properties only make sense for a single player to handle in splitscreen.
            return;
        }

        if (this.watcher is not null)
        {
            foreach (string? key in this.watcher.Check())
            {
                RecentSOManager.TryAdd(key);
            }
        }

        StatsManager.ClearProperties(); // clear property cache, repopulate at next use
        RecentSOManager.GrabNewRecentlyCompletedOrders();
        RecentSOManager.DayUpdate(Game1.stats.daysPlayed);
        RecentSOManager.Save();
    }

    /// <inheritdoc cref="IGameLoopEvents.ReturnedToTitle"/>
    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.watcher?.Dispose();
        this.watcher = null;
    }

    /// <inheritdoc cref="IGameLoopEvents.OneSecondUpdateTicked"/>
    private void OneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
    {
        if (this.watcher is null)
        {
            return;
        }

        foreach (string? key in this.watcher.Check())
        {
            RecentSOManager.TryAdd(key);
        }
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

    #region console commands

    /// <summary>
    /// Console commands to check the value of a tag.
    /// </summary>
    /// <param name="command">Name of the command.</param>
    /// <param name="args">List of tags to check.</param>
    private void ConsoleCheckTag(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            ModMonitor.Log(I18n.LoadSaveFirst(), LogLevel.Debug);
            return;
        }
        foreach (string tag in args)
        {
            string base_tag;
            ReadOnlySpan<char> span = tag.AsSpan().Trim();
            bool match = true;
            if (span.StartsWith("!"))
            {
                match = false;
                base_tag = span[1..].ToString();
            }
            else
            {
                base_tag = span.ToString();
            }
            ModMonitor.Log($"{tag}: {(match == CheckTagDelegate(base_tag) ? I18n.True() : I18n.False())}", LogLevel.Debug);
        }
    }

    /// <summary>
    /// Console command to get all available orders.
    /// </summary>
    /// <param name="command">Name of command.</param>
    /// <param name="args">Arguments for command.</param>
    private void GetAvailableOrders(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            ModMonitor.Log(I18n.LoadSaveFirst(), LogLevel.Warn);
        }
        Dictionary<string, SpecialOrderData> order_data = Game1.content.Load<Dictionary<string, SpecialOrderData>>(@"Data\SpecialOrders");
        List<string> keys = AtraUtils.ContextSort(order_data.Keys);
        ModMonitor.Log(I18n.NumberFound(count: keys.Count), LogLevel.Debug);

        List<string> validkeys = new();
        List<string> unseenkeys = new();

        foreach (string key in keys)
        {
            SpecialOrderData order = order_data[key];
            if (IsAvailableOrder(key, order))
            {
                ModMonitor.DebugOnlyLog($"\t{key} is valid");
                validkeys.Add(key);
                if (!Game1.MasterPlayer.team.completedSpecialOrders.ContainsKey(key))
                {
                    unseenkeys.Add(key);
                }
            }
        }
        ModMonitor.Log($"{I18n.ValidKeys(count: validkeys.Count)}: {string.Join(", ", validkeys)}", LogLevel.Debug);
        ModMonitor.Log($"{I18n.UnseenKeys(count: unseenkeys.Count)}: {string.Join(", ", unseenkeys)}", LogLevel.Debug);
    }

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Reviewed")]
    private static bool IsAvailableOrder(string key, SpecialOrderData order)
    {
        ModMonitor.Log($"{I18n.Analyzing()} {key}", LogLevel.Debug);
        try
        {
            SpecialOrder? specialOrder = SpecialOrder.GetSpecialOrder(key, Game1.random.Next());
            if (specialOrder is not null)
            {
                ModMonitor.Log($"\t{key} {I18n.Parsable()}", LogLevel.Debug);
                if (specialOrder.orderType.Value.Length != 0 && specialOrder.orderType.Value != "Qi")
                {
                    ModMonitor.Log($"\t\tNon-vanilla special order type {specialOrder.orderType.Value}", LogLevel.Debug);
                }
            }
            else
            {
                ModMonitor.Log($"\t{key} {I18n.Unparsable()}", LogLevel.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            ModMonitor.Log($"\t{key} {I18n.Unparsable()}\n{ex}", LogLevel.Error);
            return false;
        }

        bool seen = Game1.MasterPlayer.team.completedSpecialOrders.ContainsKey(key);
        if (order.Repeatable != "True" && seen)
        {
            ModMonitor.Log($"\t{I18n.Nonrepeatable()}", LogLevel.Debug);
            return false;
        }
        else if (seen)
        {
            ModMonitor.Log($"\t{I18n.RepeatableSeen()}", LogLevel.Debug);
        }
        if (Game1.dayOfMonth >= 16 && order.Duration == "Month")
        {
            ModMonitor.Log($"\t{I18n.MonthLongLate(cutoff: 16)}");
            return false;
        }
        if (!SpecialOrder.CheckTags(order.RequiredTags))
        {
            ModMonitor.Log($"\t{I18n.HasInvalidTags()}:", LogLevel.Debug);
            foreach (string tag in order.RequiredTags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                bool match = true;
                if (tag.Length == 0)
                {
                    continue;
                }
                string trimmed_tag;
                if (tag.StartsWith('!'))
                {
                    match = false;
                    trimmed_tag = tag[1..];
                }
                else
                {
                    trimmed_tag = tag;
                }

                if (CheckTagDelegate(trimmed_tag) != match)
                {
                    ModMonitor.Log($"\t\t{I18n.TagFailed()}: {tag}", LogLevel.Debug);
                }
            }
            return false;
        }
        foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
        {
            if (specialOrder.questKey.Value == key)
            {
                ModMonitor.Log($"\t{I18n.Active()}", LogLevel.Debug);
                return false;
            }
        }
        return true;
    }

    #endregion

    #region untimed

    /// <inheritdoc cref="IGameLoopEvents.DayEnding"/>
    private void OnDayEnd(object? sender, DayEndingEventArgs e)
    {
        CustomEmoji.Reset();

        if (Context.IsMainPlayer && Game1.player.team.specialOrders.Count > 0)
        {
            HashSet<string> overrides = AssetManager.Untimed.Value;
            if (overrides.Count == 0)
            {
                return;
            }
            WorldDate? date = new(Game1.year, Game1.currentSeason, Game1.dayOfMonth);
            foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
            {
                if (overrides.Contains(specialOrder.questKey.Value) && specialOrder.GetDaysLeft() < 50)
                {
                    this.Monitor.Log($"Overriding duration of untimed special order {specialOrder.questKey.Value}");
                    specialOrder.dueDate.Value = date.TotalDays + 99;
                }
            }
        }
    }
    #endregion
}
