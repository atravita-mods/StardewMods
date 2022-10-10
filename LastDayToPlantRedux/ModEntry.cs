using AtraShared.Integrations;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.Shims;
using LastDayToPlantRedux.Framework;
using StardewModdingAPI.Events;

using AtraUtils = AtraShared.Utils.Utils;

namespace LastDayToPlantRedux;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <summary>
    /// Gets the game content helper for this mod.
    /// </summary>
    internal static IGameContentHelper GameContentHelper { get; private set; } = null!;

    /// <summary>
    /// Gets the config instance for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // bind helpers.
        ModMonitor = this.Monitor;
        GameContentHelper = this.Helper.GameContent;

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += this.OnDayStart;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTile;

        helper.Events.Multiplayer.PeerConnected += (_, e) => MultiplayerManager.OnPlayerConnected(e);
        helper.Events.Multiplayer.PeerDisconnected += (_, e) => MultiplayerManager.OnPlayerDisconnected(e);

        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.Content.AssetsInvalidated += this.OnAssetsInvalidated;
    }

    [EventPriority(EventPriority.High + 10)]
    private void OnReturnedToTile(object? sender, ReturnedToTitleEventArgs e)
    {
        InventoryWatcher.ClearModel();
        MultiplayerManager.Reset();

        this.Helper.Events.Player.InventoryChanged -= this.OnInventoryChange;
        this.Helper.Events.GameLoop.Saving -= this.OnSaving;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Config.CropsToDisplay == CropOptions.Purchaseable)
        {
            // set up JA integration
        }

        InventoryWatcher.LoadModel(this.Helper.Data);

        this.Helper.Events.Player.InventoryChanged -= this.OnInventoryChange;
        this.Helper.Events.Player.InventoryChanged += this.OnInventoryChange;

        this.Helper.Events.GameLoop.Saving -= this.OnSaving;
        this.Helper.Events.GameLoop.Saving += this.OnSaving;
    }

    private void OnDayStart(object? sender, DayStartedEventArgs e)
    {
         MultiplayerManager.UpdateOnDayStart(e);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        MultiplayerManager.SetShouldCheckPrestiged(this.Helper.ModRegistry);

        // Ask for AtraCore's JAShims to be initialized.
        JsonAssetsShims.Initialize(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry);

        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (helper.TryGetAPI())
        {
            helper.Register(
                reset: static () => Config = new(),
                save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config),
                titleScreenOnly: true)
            .GenerateDefaultGMCM(static () => Config);
        }
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        => AssetManager.Apply(e);

    private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        => AssetManager.InvalidateCache(e);

    private void OnInventoryChange(object? sender, InventoryChangedEventArgs e)
        => InventoryWatcher.Watch(e, this.Helper.Data);

    private void OnSaving(object? sender, SavingEventArgs e)
        => InventoryWatcher.SaveModel(this.Helper.Data);
}
