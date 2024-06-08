namespace StopRugRemoval;

using System.Diagnostics;

using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.Caches;
using AtraCore.Framework.QueuePlayerAlert;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Menuing;
using AtraShared.MigrationManager;
using AtraShared.Schedules;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;

using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;

using StopRugRemoval.Configuration;
using StopRugRemoval.Framework.Niceties;
using StopRugRemoval.HarmonyPatches;
using StopRugRemoval.HarmonyPatches.Confirmations;
using StopRugRemoval.HarmonyPatches.Niceties;
using StopRugRemoval.HarmonyPatches.Niceties.PhoneTiming;
using StopRugRemoval.HarmonyPatches.Volcano;

using AtraUtils = AtraShared.Utils.Utils;

/// <summary>
/// Entry class to the mod.
/// </summary>
internal sealed class ModEntry : Mod
{
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1306:Field names should begin with lower-case letter", Justification = "Reviewed.")]
    private static GMCMHelper? GMCM = null;

    private MigrationManager? migrator;

    #region accessors

    /// <summary>
    /// Gets the logger for this file.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <summary>
    /// Gets instance that holds the configuration for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <summary>
    /// Gets the multiplayer helper for this mod.
    /// </summary>
    internal static IMultiplayerHelper MultiplayerHelper { get; private set; } = null!;

    /// <summary>
    /// Gets the input helper for this mod.
    /// </summary>
    internal static IInputHelper InputHelper { get; private set; } = null!;

    /// <summary>
    /// Gets the unique id for this mod.
    /// </summary>
    internal static string UNIQUEID { get; private set; } = null!;

    /// <summary>
    /// Gets the instance of the schedule utility functions.
    /// </summary>
    internal static ScheduleUtilityFunctions UtilitySchedulingFunctions { get; private set; } = null!;

    #endregion

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        AssetEditor.Initialize(helper.GameContent);
        ModMonitor = this.Monitor;
        MultiplayerHelper = this.Helper.Multiplayer;
        InputHelper = this.Helper.Input;
        UNIQUEID = this.ModManifest.UniqueID;

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);
        if (Config.MaxNoteChance < Config.MinNoteChance)
        {
            (Config.MaxNoteChance, Config.MinNoteChance) = (Config.MinNoteChance, Config.MaxNoteChance);
            helper.AsyncWriteConfig(ModEntry.ModMonitor, Config);
        }

        UtilitySchedulingFunctions = new(this.Monitor, this.Helper.Translation);

        this.Monitor.Log($"Starting up: {this.ModManifest.UniqueID} - {typeof(ModEntry).Assembly.FullName}");

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunch;
        helper.Events.GameLoop.SaveLoaded += this.SaveLoaded;
        helper.Events.GameLoop.Saving += this.BeforeSaving;
        helper.Events.Player.Warped += this.Player_Warped;
        helper.Events.GameLoop.ReturnedToTitle += this.ReturnedToTitle;

        helper.Events.Input.ButtonPressed += this.OnButtonPressed;

        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.Content.AssetsInvalidated += this.OnAssetInvalidated;
        helper.Events.Content.LocaleChanged += this.OnLocaleChange;

        helper.Events.Multiplayer.ModMessageReceived += this.OnModMessageRecieved;
        helper.Events.Multiplayer.PeerConnected += this.OnPlayerConnected;

        helper.Events.Specialized.LoadStageChanged += this.OnLoadStageChanged;

        // secret notes
        FixSecretNotes.Initialize(helper.GameContent);
        helper.Events.Content.AssetsInvalidated += (_, e) => FixSecretNotes.Reset(e.NamesWithoutLocale);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AssetEditor.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <inheritdoc cref="IInputEvents.ButtonPressed"/>
    [EventPriority(EventPriority.High)]
    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (MenuingExtensions.IsNormalGameplay() && Config.Enabled)
        {
            _ = RecipeConsumer.ConsumeRecipeIfNeeded(e, this.Helper.Input)
                || CropAndTreeFlipper.OnButtonPressed(e, this.Helper.Input)
                || ChestSwitcher.Switch(e, this.Helper.Input);
        }
    }

    /// <inheritdoc cref="ISpecializedEvents.LoadStageChanged"/>
    [EventPriority(EventPriority.Low)]
    private void OnLoadStageChanged(object? sender, LoadStageChangedEventArgs e)
    {
        if (e.NewStage is LoadStage.SaveLoadedLocations)
        {
            foreach (Farmer player in Game1.getAllFarmers())
            {
                if (player.Items.Count < player.MaxItems)
                {
                    this.Monitor.Log("Detected broken inventory, fixing.", LogLevel.Warn);
                    for (int i = player.Items.Count; i < player.MaxItems; i++)
                    {
                        player.Items.Add(null);
                    }
                }

                this.Monitor.Log($"Checking for nulls in player quest log for {player.Name}");
                player.questLog.ClearNulls();
            }

            this.Monitor.VerboseLog($"Attempting to clear nulls from returned donations");
            Game1.player.team.returnedDonations.ClearNulls();

            foreach (GameLocation? location in Game1.locations)
            {
                if (location is AnimalHouse)
                {
                    continue;
                }

                foreach ((long id, FarmAnimal? animal) in location.animals.Pairs)
                {
                    if (animal.home is null)
                    {
                        this.Monitor.Log($"{animal.displayName} appears to be homeless.", LogLevel.Warn);
                        foreach (Building? building in location.buildings)
                        {
                            if (building.GetIndoors() is AnimalHouse animalHouse && animalHouse.animalsThatLiveHere.Contains(id))
                            {
                                animal.home = building;
                                this.Monitor.Log($"{animal.displayName} assigned home {animalHouse.NameOrUniqueName}", LogLevel.Info);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    #region asset edits

    /// <inheritdoc cref="IContentEvents.LocaleChanged"/>
    private void OnLocaleChange(object? sender, LocaleChangedEventArgs e)
        => AssetEditor.Refresh();

    /// <inheritdoc cref="IContentEvents.AssetsInvalidated"/>
    private void OnAssetInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        => AssetEditor.Refresh(e.NamesWithoutLocale);

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        => AssetEditor.Edit(e, this.Helper.DirectoryPath);

    /// <summary>
    /// Edits the saloon event.
    /// </summary>
    /// <param name="sender">SMAPI.</param>
    /// <param name="e">event args.</param>
    /// <remarks>Not hooked if specific other mods are installed.</remarks>
    private void OnSaloonEventRequested(object? sender, AssetRequestedEventArgs e)
        => AssetEditor.EditSaloonEvent(e);

    #endregion

    /// <inheritdoc cref="IPlayerEvents.Warped"/>
    private void Player_Warped(object? sender, WarpedEventArgs e)
    {
        SObjectPatches.HaveConfirmedBomb.Value = false;
        ConfirmWarp.HaveConfirmed.Value = false;
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll(typeof(ModEntry).Assembly);
            if (!this.Helper.ModRegistry.IsLoaded("DecidedlyHuman.BetterReturnScepter"))
            {
                ConfirmWarp.ApplyWandPatches(harmony);
            }
            if (this.Helper.ModRegistry.IsLoaded("Becks723.PhoneTravelingCart"))
            {
                this.Monitor.Log("Phone Traveling Cart found, patching for compat", LogLevel.Info);
                ScaleDialing.ApplyPatches(harmony);
            }
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    private void OnGameLaunch(object? sender, GameLaunchedEventArgs e)
    {
        PlantGrassUnder.GetSmartBuildingBuildMode(this.Helper.Translation, this.Helper.ModRegistry);
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

        GMCM = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (GMCM.TryGetAPI())
        {
            this.SetUpBasicConfig();
        }

        // NPC sanity checking
        this.Helper.Events.GameLoop.DayStarted += static (_, _) => DuplicateNPCDetector.DayStart();

        if (!this.Helper.ModRegistry.IsLoaded("violetlizabet.CP.NoAlcohol"))
        {
            this.Helper.Events.Content.AssetRequested += this.OnSaloonEventRequested;
        }
        else
        {
            this.Monitor.Log("violetlizabet.CP.NoAlcohol detected, not editing saloon event.");
        }
    }

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    private void SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        // This allows NPCs to say hi to the player. Yes, I'm that petty.
        Game1.player.displayName = Game1.player.Name;

        // Crosscheck the player's spouse is still valid.
        ValidateSpouse();

        if (Context.IsSplitScreen && Context.ScreenId != 0)
        {
            return;
        }

        // Have to wait until here to populate locations
        if (Config.PrePopulateLocations())
        {
            this.Helper.AsyncWriteConfig(this.Monitor, Config);
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

        if (GMCM?.HasGottenAPI == true)
        {
            GMCM.AddPageHere("Bombs", I18n.BombLocationDetailed)
                .AddParagraph(I18n.BombLocationDetailed_Description);

            foreach ((string name, IsSafeLocationEnum setting) in Config.SafeLocationMap)
            {
                GMCM.AddEnumOption(
                    name: () => name,
                    getValue: () => Config.SafeLocationMap.TryGetValue(name, out IsSafeLocationEnum val) ? val : IsSafeLocationEnum.Dynamic,
                    setValue: (value) => Config.SafeLocationMap[name] = value);
            }
        }

        // fix the railroad being silly
        if (Game1.getLocationFromName("Railroad") is Railroad railroad)
        {
            try
            {
                railroad.DayUpdate(Game1.dayOfMonth);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Could not force railroad to update at save load:\n{ex}");
            }
        }

        if (Context.IsMainPlayer)
        {
            VolcanoChestAdjuster.LoadData(this.Helper.Data, this.Helper.Multiplayer);

#if DEBUG
            Stopwatch sw = Stopwatch.StartNew();
#endif

            Utility.ForEachLocation(action: static (GameLocation loc) =>
            {
                if (loc is null)
                {
                    return true;
                }

                // crosscheck and remove nulls in GameLocation.characters
                // Game and mods do not like this.
                if (loc.characters is not null)
                {
                    for (int i = loc.characters.Count - 1; i >= 0; i--)
                    {
                        if (loc.characters[i] is null)
                        {
                            ModMonitor.Log($"Found null in characters list for {loc.NameOrUniqueName}, removing.", LogLevel.Warn);
                            loc.characters.RemoveAt(i);
                        }
                    }
                }

                // Make an attempt to clear all nulls from chests.
                foreach (SObject obj in loc.Objects.Values)
                {
                    if (obj is Chest chest)
                    {
                        chest.clearNulls();
                    }
                }

                if (loc is FarmHouse house)
                {
                    house.fridge?.Value?.clearNulls();
                }
                else if (loc is IslandFarmHouse islandHouse)
                {
                    islandHouse.fridge?.Value?.clearNulls();
                }

                return true;
            });
#if DEBUG
            this.Monitor.LogTimespan("Sanity checking locations", sw, LogLevel.Trace);
#endif
        }
    }

    /// <summary>
    /// Check to see if a player's spouse data is still valid.
    /// </summary>
    private static void ValidateSpouse()
    {
        const string modData = "atravita.RememberedSpouse";
        if (Game1.player.spouse is not null && NPCCache.GetByVillagerName(Game1.player.spouse, searchTheater: true) is null)
        {
            string spouseName = Game1.player.spouse;
            ModMonitor.Log($"Player married to {spouseName} but spouse instance not found.", LogLevel.Warn);
            if (Game1.characterData.ContainsKey(spouseName))
            {
                ModMonitor.Log($"{spouseName} accounted for NPCDispos. We expect them to be respawned later.", LogLevel.Info);
            }
            else
            {
                ModMonitor.Log($"Cannot account for NPC spouse {spouseName}. Did you remove an NPC mod? Setting spouse to null.", LogLevel.Warn);
                Game1.player.modData[modData] = Game1.player.spouse;
                Game1.player.spouse = null;
                PlayerAlertHandler.AddMessage(new(I18n.EmergencyDivorce_Message(spouseName), HUDMessage.error_type));
            }
        }
        else if (!Game1.player.team.IsMarried(Game1.player.UniqueMultiplayerID) // player marriage.
            && Game1.player.spouse is null && Game1.player.modData.TryGetValue(modData, out string? pastSpouse)
            && Game1.player.friendshipData.TryGetValue(pastSpouse, out Friendship? friendship) && friendship.IsMarried())
        {
            ModMonitor.Log($"Checking past spouse {pastSpouse}.");
            if (NPCCache.GetByVillagerName(pastSpouse, searchTheater: true) is not null)
            {
                ModMonitor.Log($"Past spouse found! Re-initiating marriage.", LogLevel.Info);
                Game1.player.spouse = pastSpouse;
                Game1.player.modData.Remove(modData);
            }
        }
    }

    private void BeforeSaving(object? sender, SavingEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            VolcanoChestAdjuster.SaveData(this.Helper.Data, this.Helper.Multiplayer);
        }
    }

    /// <summary>
    /// Writes migration data then detaches the migrator.
    /// </summary>
    /// <param name="sender">Smapi thing.</param>
    /// <param name="e">Arguments for just-before-saving.</param>
    private void WriteMigrationData(object? sender, SavedEventArgs e)
    {
        if (this.migrator is not null)
        {
            this.migrator.SaveVersionInfo();
            this.migrator = null;
        }
        this.Helper.Events.GameLoop.Saved -= this.WriteMigrationData;
    }

    #region GMCM

    /// <inheritdoc cref="IGameLoopEvents.ReturnedToTitle"/>
    private void ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        if (GMCM?.HasGottenAPI == true)
        {
            GMCM.Unregister();
            this.SetUpBasicConfig();
        }
    }

    private void SetUpBasicConfig()
    {
        GMCM!.Register(
                reset: static () =>
                {
                    Config = new ModConfig();
                    Config.PrePopulateLocations();
                },
                save: () =>
                {
                    if (Config.MaxNoteChance < Config.MinNoteChance)
                    {
                        (Config.MaxNoteChance, Config.MinNoteChance) = (Config.MinNoteChance, Config.MaxNoteChance);
                    }
                    this.Helper.AsyncWriteConfig(this.Monitor, Config);
                })
            .AddParagraph(I18n.Mod_Description)
            .GenerateDefaultGMCM(static () => Config);

        GMCM!.AddSectionTitle(I18n.ConfirmWarps_Title)
            .AddParagraph(I18n.ConfirmWarps_Description)
            .AddEnumOption(
                name: I18n.WarpsInSafeAreas_Title,
                getValue: static () => Config.WarpsInSafeAreas,
                setValue: static (value) => Config.WarpsInSafeAreas = value,
                tooltip: I18n.WarpsInSafeAreas_Description)
            .AddEnumOption(
                name: I18n.WarpsInDangerousAreas_Title,
                getValue: static () => Config.WarpsInDangerousAreas,
                setValue: static (value) => Config.WarpsInDangerousAreas = value,
                tooltip: I18n.WarpsInDangerousAreas_Description);

        GMCM!.AddSectionTitle(I18n.ConfirmScepter_Title);
        if (this.Helper.ModRegistry.IsLoaded("DecidedlyHuman.BetterReturnScepter"))
        {
            GMCM!.AddParagraph(I18n.BetterReturnScepter);
        }
        else
        {
            GMCM!.AddParagraph(I18n.ConfirmScepter_Description)
                .AddEnumOption(
                    name: I18n.ReturnScepterInSafeAreas_Title,
                    getValue: static () => Config.ReturnScepterInSafeAreas,
                    setValue: static (value) => Config.ReturnScepterInSafeAreas = value,
                    tooltip: I18n.ReturnScepterInSafeAreas_Description)
                .AddEnumOption(
                    name: I18n.ReturnScepterInDangerousAreas_Title,
                    getValue: static () => Config.ReturnScepterInDangerousAreas,
                    setValue: static (value) => Config.ReturnScepterInDangerousAreas = value,
                    tooltip: I18n.ReturnScepterInDangerousAreas_Description);
        }

        GMCM!.AddSectionTitle(I18n.ConfirmBomb_Title)
            .AddParagraph(I18n.ConfirmBomb_Description)
            .AddEnumOption(
                name: I18n.BombsInSafeAreas_Title,
                getValue: static () => Config.BombsInSafeAreas,
                setValue: static (value) => Config.BombsInSafeAreas = value,
                tooltip: I18n.BombsInSafeAreas_Description)
            .AddEnumOption(
                name: I18n.BombsInDangerousAreas_Title,
                getValue: static () => Config.BombsInDangerousAreas,
                setValue: static (value) => Config.BombsInDangerousAreas = value,
                tooltip: I18n.BombsInDangerousAreas_Description);
    }

    #endregion

    #region multiplayer

    /// <inheritdoc cref="IMultiplayerEvents.ModMessageReceived"/>
    private void OnModMessageRecieved(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != UNIQUEID)
        {
            return;
        }
        VolcanoChestAdjuster.RecieveData(e);
    }

    /// <inheritdoc cref="IMultiplayerEvents.PeerConnected"/>
    /// <remarks>
    /// Sends out the volcano data manager whenever a new player connects.
    /// </remarks>
    private void OnPlayerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if(e.Peer.ScreenID == 0 && Context.IsWorldReady && Context.IsMainPlayer)
        {
            VolcanoChestAdjuster.BroadcastData(this.Helper.Multiplayer, new[] { e.Peer.PlayerID });
        }
    }

    #endregion
}
