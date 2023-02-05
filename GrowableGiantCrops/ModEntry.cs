using AtraCore.Utilities;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.MigrationManager;
using AtraShared.Utils.Extensions;

using GrowableGiantCrops.Framework;
using GrowableGiantCrops.Framework.Assets;
using GrowableGiantCrops.Framework.InventoryModels;
using GrowableGiantCrops.HarmonyPatches.Compat;
using GrowableGiantCrops.HarmonyPatches.ItemPatches;
using GrowableGiantCrops.HarmonyPatches.Niceties;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

using AtraUtils = AtraShared.Utils.Utils;

namespace GrowableGiantCrops;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private static int[]? moreGiantCropsIds;
    private static int[]? jaCropIds;

    private MigrationManager? migrator;

    /// <summary>
    /// Gets the product IDs recognized by More Giant Crops.
    /// </summary>
    internal static int[] MoreGiantCropsIds => moreGiantCropsIds ?? Array.Empty<int>();

    /// <summary>
    /// Gets the product IDs recognized by JsonAssets.
    /// </summary>
    internal static int[] JACropIds => jaCropIds ?? Array.Empty<int>();

    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <summary>
    /// Gets the config instance for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    #region APIs
    internal static IJsonAssetsAPI? JaAPI { get; private set; }

    internal static IMoreGiantCropsAPI? MoreGiantCropsAPI { get; private set; }

    internal static IGiantCropTweaks? GiantCropTweaksAPI { get; private set; }

    internal static IGrowableBushesAPI? GrowableBushesAPI { get; private set; }
    #endregion

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        this.Monitor.Log($"Starting up: {this.ModManifest.UniqueID} - {typeof(ModEntry).Assembly.FullName}");

        ModMonitor = this.Monitor;
        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        ConsoleCommands.RegisterCommands(helper.ConsoleCommands);

        AssetManager.Initialize(helper.GameContent);
        AssetCache.Initialize(helper.GameContent);

        ShopManager.Initialize(helper.GameContent);

        // assets
        this.Helper.Events.Content.AssetRequested += static (_, e) => AssetManager.OnAssetRequested(e);
        this.Helper.Events.Content.AssetsInvalidated += static (_, e) => AssetManager.Reset(e.NamesWithoutLocale);

        this.Helper.Events.Content.AssetsInvalidated += static (_, e) => AssetCache.Refresh(e.NamesWithoutLocale);
        this.Helper.Events.Content.AssetReady += static (_, e) => AssetCache.Ready(e.NameWithoutLocale);
    }

    /// <inheritdoc />
    public override object? GetApi() => new Api();

    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        IntegrationHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, LogLevel.Error);

        if (!helper.TryGetAPI("spacechase0.SpaceCore", "1.9.3", out ICompleteSpaceCoreAPI? api))
        {
            this.Monitor.Log($"Could not load spacecore's API. This is a fatal error.", LogLevel.Error);
            return;
        }

        api.RegisterSerializerType(typeof(InventoryGiantCrop));
        api.RegisterSerializerType(typeof(ShovelTool));
        api.RegisterSerializerType(typeof(InventoryResourceClump));

        this.Helper.Events.GameLoop.SaveLoaded += this.SaveLoaded;

        // shop
        this.Helper.Events.Content.AssetRequested += static (_, e) => ShopManager.OnAssetRequested(e);
        this.Helper.Events.Content.AssetsInvalidated += static (_, e) => ShopManager.OnAssetInvalidated(e.NamesWithoutLocale);
        // this.Helper.Events.GameLoop.DayEnding += static (_, _) => ShopManager.OnDayEnd();
        // this.Helper.Events.Input.ButtonPressed += (_, e) => ShopManager.OnButtonPressed(e, this.Helper.Input);

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

        GMCMHelper gmcmHelper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);

        if (gmcmHelper.TryGetAPI())
        {
            gmcmHelper.Register(
                reset: static () => Config = new(),
                save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config))
            .AddParagraph(I18n.ModDescription)
            .GenerateDefaultGMCM(static () => Config)
            .AddTextOption(
                name: I18n.ResourceShopLocation_Title,
                getValue: static () => Config.ResourceShopLocation.X + ", " + Config.ResourceShopLocation.Y,
                setValue: static (str) => Config.ResourceShopLocation = str.TryParseVector2(out Vector2 vec) ? vec : new Vector2(6, 19),
                tooltip: I18n.ResourceShopLocation_Description)
            .AddTextOption(
                name: I18n.GiantCropShopLocation_Title,
                getValue: static () => Config.GiantCropShopLocation.X + ", " + Config.GiantCropShopLocation.Y,
                setValue: static (str) => Config.GiantCropShopLocation = str.TryParseVector2(out Vector2 vec) ? vec : new Vector2(8, 14),
                tooltip: I18n.GiantCropShopLocation_Description);
        }

        // optional APIs
        IntegrationHelper optional = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, LogLevel.Trace);
        if (optional.TryGetAPI("spacechase0.JsonAssets", "1.10.10", out IJsonAssetsAPI? jaAPI))
        {
            JaAPI = jaAPI;
        }
        if (optional.TryGetAPI("spacechase0.MoreGiantCrops", "1.2.0", out IMoreGiantCropsAPI? mgAPI))
        {
            MoreGiantCropsAPI = mgAPI;
        }
        if (optional.TryGetAPI("leclair.giantcroptweaks", "0.1.0", out IGiantCropTweaks? gcAPI))
        {
            GiantCropTweaksAPI = gcAPI;
        }
        if (optional.TryGetAPI("atravita.GrowableBushes", "0.0.1", out IGrowableBushesAPI? growable))
        {
            GrowableBushesAPI = growable;
        }
    }

    /// <summary>
    /// Applies the patches for this mod.
    /// </summary>
    /// <param name="harmony">This mod's harmony instance.</param>
    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll(typeof(ModEntry).Assembly);

            if (new Version(1, 6) > new Version(Game1.version))
            {
                this.Monitor.Log("Applying patch to restore giant crops and clumps to save locations", LogLevel.Debug);
                FixSaveThing.ApplyPatches(harmony);
            }

            if (this.Helper.ModRegistry.IsLoaded("spacechase0.JsonAssets"))
            {
                this.Monitor.Log("Applying deshuffle patch");
                DeshufflePatch.ApplyPatch(harmony);
            }

            if (this.Helper.ModRegistry.Get("Esca.FarmTypeManager") is IModInfo ftm
                && !ftm.Manifest.Version.IsOlderThan("1.16.0"))
            {
                this.Monitor.Log("Applying FTM patches");
                FTMArtifactSpotPatch.ApplyPatch(harmony);
            }
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    #region migration

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    /// <remarks>Used to load in this mod's data models.</remarks>
    private void SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        // load giant crop indexes.
        moreGiantCropsIds = MoreGiantCropsAPI?.RegisteredCrops() ?? Array.Empty<int>();
        jaCropIds = JaAPI?.GetGiantCropIndexes() ?? Array.Empty<int>();

        // sanity checks.
        MultiplayerHelpers.AssertMultiplayerVersions(this.Helper.Multiplayer, this.ModManifest, this.Monitor, this.Helper.Translation);

        if (Context.IsSplitScreen && Context.ScreenId != 0)
        {
            return;
        }

        // migration
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
    #endregion
}