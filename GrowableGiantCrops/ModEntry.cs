// Ignore Spelling: Api

using System.Diagnostics;

using AtraCore.Framework.Internal;
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

using HarmonyLib;

using StardewModdingAPI.Events;

using AtraUtils = AtraShared.Utils.Utils;

namespace GrowableGiantCrops;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    private static PalmTreeBehavior lastPalmTreeBehavior;

    private MigrationManager? migrator;

    /// <summary>
    /// Gets the config instance for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    internal static IGrowableBushesAPI? GrowableBushesAPI { get; private set; }

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        base.Entry(helper);

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);
        lastPalmTreeBehavior = Config.PalmTreeBehavior;

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        ConsoleCommands.RegisterCommands(helper.ConsoleCommands);

        AssetManager.Initialize(helper.GameContent);
        AssetCache.Initialize(helper.GameContent);

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

        api.RegisterSerializerType(typeof(ShovelTool));
        api.RegisterSerializerType(typeof(InventoryResourceClump));
        api.RegisterSerializerType(typeof(InventoryGiantCrop));
        api.RegisterSerializerType(typeof(InventoryFruitTree));
        api.RegisterSerializerType(typeof(InventoryTree));

        this.Helper.Events.GameLoop.SaveLoaded += this.SaveLoaded;

        // invalidations
        AssetManager.Invalidate(this.Helper.GameContent);

        // shop
        ShopManager.Initialize(this.Helper.GameContent);
        this.Helper.Events.Content.AssetRequested += static (_, e) => ShopManager.OnAssetRequested(e);
        this.Helper.Events.Content.AssetsInvalidated += static (_, e) => ShopManager.OnAssetInvalidated(e.NamesWithoutLocale);
        this.Helper.Events.Input.ButtonPressed += (_, e) => ShopManager.OnButtonPressed(e, this.Helper.Input);
        this.Helper.Events.GameLoop.DayEnding += static (_, _) => ShopManager.OnDayEnd();
        this.Helper.Events.GameLoop.ReturnedToTitle += static (_, _) => ShopManager.Reset(true);
        this.Helper.Events.Player.Warped += static (_, e) => ShopManager.AddBoxToShop(e);

        // trees - season switching in inventory.
        this.Helper.Events.Player.Warped += this.OnPlayerWarped;
        this.Helper.Events.Player.InventoryChanged += this.OnInventoryChanged;

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

        GMCMHelper gmcmHelper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);

        if (gmcmHelper.TryGetAPI())
        {
            gmcmHelper.Register(
                reset: () =>
                {
                    Config = new();
                    this.UpdateForPalmTrees();
                },
                save: () =>
                {
                    this.Helper.AsyncWriteConfig(this.Monitor, Config);
                    this.UpdateForPalmTrees();
                })
            .AddParagraph(I18n.ModDescription)
            .GenerateDefaultGMCM(static () => Config);
        }

        // optional APIs
        {
            IntegrationHelper optional = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, LogLevel.Trace);
            if (optional.TryGetAPI("atravita.GrowableBushes", "0.0.1", out IGrowableBushesAPI? growable))
            {
                GrowableBushesAPI = growable;
            }
        }
    }

    private void UpdateForPalmTrees()
    {
        if (lastPalmTreeBehavior != Config.PalmTreeBehavior)
        {
            AssetManager.Invalidate(this.Helper.GameContent);
            lastPalmTreeBehavior = Config.PalmTreeBehavior;
        }
    }

    #region resetting

    private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
    {
        if (!e.IsLocalPlayer)
        {
            return;
        }

        foreach (Item item in e.Added)
        {
            GGCUtils.ResetGraphics(item);
        }

        foreach (Item item in e.Removed)
        {
            GGCUtils.ResetGraphics(item);
        }
    }

    private void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer)
        {
            return;
        }

        foreach (Item? item in e.Player.Items)
        {
            GGCUtils.ResetGraphics(item);
        }
    }

    #endregion

    /// <summary>
    /// Applies the patches for this mod.
    /// </summary>
    /// <param name="harmony">This mod's harmony instance.</param>
    private void ApplyPatches(Harmony harmony)
    {
#if DEBUG
        Stopwatch sw = Stopwatch.StartNew();
#endif
        try
        {
            harmony.PatchAll(typeof(ModEntry).Assembly);

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
            if (this.Helper.ModRegistry.IsLoaded("spacechase0.MoreGrassStarters"))
            {
                this.Monitor.Log("Patching More Grass Starters");
                MoreGrassStartersCompat.ApplyPatch(harmony, this.Helper.ModRegistry);
            }

            if (this.Helper.ModRegistry.IsLoaded("exotico.SlimeProduce"))
            {
                this.Monitor.Log("Patching Slime Produce");
                SlimeProduceCompat.ApplyPatches(harmony);
            }
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);

#if DEBUG
        sw.Stop();
        this.Monitor.LogTimespan("Applying harmony patches", sw);
#endif
    }

    #region migration

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    /// <remarks>Used to load in this mod's data models.</remarks>
    private void SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
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