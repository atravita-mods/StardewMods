using AtraShared.Integrations;
using AtraShared.MigrationManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.Shims;
using LastDayToPlantRedux.Framework;
using StardewModdingAPI.Events;

using AtraUtils = AtraShared.Utils.Utils;

namespace LastDayToPlantRedux;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private MigrationManager? migrator;

    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <summary>
    /// Gets the config instance for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // bind helpers.
        ModMonitor = this.Monitor;
        AssetManager.Initialize(helper.GameContent);

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += static (_, _) => AssetManager.UpdateOnDayStart();
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTile;

        helper.Events.Player.InventoryChanged += (_, e) => InventoryWatcher.Watch(e, helper.Data);
        helper.Events.GameLoop.Saving += (_, _) => InventoryWatcher.SaveModel(helper.Data);

        helper.Events.Multiplayer.PeerConnected += static (_, e) => MultiplayerManager.OnPlayerConnected(e);
        helper.Events.Multiplayer.PeerDisconnected += static (_, e) => MultiplayerManager.OnPlayerDisconnected(e);

        helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Apply(e);
        helper.Events.Content.AssetsInvalidated += static (_, e) => AssetManager.InvalidateCache(e);
    }

    /// <inheritdoc cref="IGameLoopEvents.ReturnedToTitle"/>
    [EventPriority(EventPriority.High + 10)]
    private void OnReturnedToTile(object? sender, ReturnedToTitleEventArgs e)
    {
        CropAndFertilizerManager.RequestInvalidateCrops();
        CropAndFertilizerManager.RequestInvalidateFertilizers();
        InventoryWatcher.ClearModel();
        MultiplayerManager.Reset();
    }

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        InventoryWatcher.LoadModel(this.Helper.Data);

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

    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
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
}
