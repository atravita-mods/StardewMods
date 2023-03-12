using AtraCore.Utilities;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.MigrationManager;
using AtraShared.Utils.Extensions;

using CritterRings.Framework;
using CritterRings.Models;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

using StardewValley.BellsAndWhistles;
using StardewValley.TerrainFeatures;

using AtraUtils = AtraShared.Utils.Utils;

namespace CritterRings;

/// <inheritdoc />
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Reviewed.")]
internal sealed class ModEntry : Mod
{
    internal const int BuffID = 2731247;

    private const string SAVEKEY = "item_ids";

    private static IJsonAssetsAPI? jsonAssets;
    private MigrationManager? migrator;

    private PerScreen<Vector2> playerPosition = new();

    private PerScreen<WeakReference<Bush>> bestBush = new();

    /// <summary>
    /// Gets the config instance for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    #region JA ids

    private static int bunnyRing = -1;

    /// <summary>
    /// Gets the integer Id of the Bunny Ring. -1 if not found/not loaded yet.
    /// </summary>
    internal static int BunnyRing
    {
        get
        {
            if (bunnyRing == -1)
            {
                bunnyRing = jsonAssets?.GetObjectId("atravita.BunnyRing") ?? -1;
            }
            return bunnyRing;
        }
    }

    private static int butterflyRing = -1;

    /// <summary>
    /// Gets the integer Id of the Butterfly Ring. -1 if not found/not loaded yet.
    /// </summary>
    internal static int ButterflyRing
    {
        get
        {
            if (butterflyRing == -1)
            {
                butterflyRing = jsonAssets?.GetObjectId("atravita.ButterflyRing") ?? -1;
            }
            return butterflyRing;
        }
    }

    private static int fireflyRing = -1;

    /// <summary>
    /// Gets the integer Id of the FireFly Ring. -1 if not found/not loaded yet.
    /// </summary>
    internal static int FireFlyRing
    {
        get
        {
            if (fireflyRing == -1)
            {
                fireflyRing = jsonAssets?.GetObjectId("atravita.FireFlyRing") ?? -1;
            }
            return fireflyRing;
        }
    }

    private static int owlRing = -1;

    /// <summary>
    /// Gets the integer Id of the Owl Ring. -1 if not found/not loaded yet.
    /// </summary>
    internal static int OwlRing
    {
        get
        {
            if (owlRing == -1)
            {
                owlRing = jsonAssets?.GetObjectId("atravita.OwlRing") ?? -1;
            }
            return owlRing;
        }
    }

    #endregion

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModMonitor = this.Monitor;
        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        this.Monitor.Log($"Starting up: {this.ModManifest.UniqueID} - {typeof(ModEntry).Assembly.FullName}");
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
        AssetManager.Initialize(helper.GameContent);
    }

    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        {
            IntegrationHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, LogLevel.Warn);
            if (!helper.TryGetAPI("spacechase0.JsonAssets", "1.10.3", out jsonAssets))
            {
                this.Monitor.Log("Packs could not be loaded! This mod will probably not function.", LogLevel.Error);
                return;
            }
            jsonAssets.LoadAssets(Path.Combine(this.Helper.DirectoryPath, "assets", "json-assets"), this.Helper.Translation);
        }

        this.Helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        this.Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        this.Helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        this.Helper.Events.Player.Warped += this.OnWarp;
        this.Helper.Events.Input.ButtonPressed += this.OnButtonPressed;

        this.Helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Apply(e);
        this.Helper.Events.Content.AssetsInvalidated += static (_, e) => AssetManager.Reset(e.NamesWithoutLocale);

        GMCMHelper gmcmHelper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (gmcmHelper.TryGetAPI())
        {
            gmcmHelper.Register(
                reset: static () => Config = new(),
                save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config))
            .AddParagraph(I18n.Mod_Description)
            .GenerateDefaultGMCM(static () => Config);
        }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (Context.IsPlayerFree && Config.BunnyRingButton.JustPressed())
        {
            if (!Game1.player.hasBuff(BuffID) && Game1.player.Stamina > 0 && !Game1.player.exhausted.Value)
            {
                Buff buff = BuffEnum.Speed.GetBuffOf(3, 20, "atravita.BunnyRing", I18n.BunnyRing_Name());
                buff.which = BuffID;
                buff.description = I18n.BunnyBuff_Description();
                buff.sheetIndex = 1;

                Game1.buffsDisplay.addOtherBuff(buff);
                Game1.player.Stamina -= Config.BunnyRingStamina;
            }
        }
    }

    /// <inheritdoc cref="IPlayerEvents.Warped"/>
    private void OnWarp(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer)
        {
            return;
        }
        e.NewLocation?.instantiateCrittersList();
        if (e.NewLocation?.critters is not List<Critter> critters)
        {
            return;
        }
        if (Game1.isDarkOut())
        {
            if (FireFlyRing > 0 && Game1.player.isWearingRing(FireFlyRing))
            {
                CRUtils.SpawnFirefly(critters, 3);
            }
        }
        else if (Game1.currentLocation.ShouldSpawnButterflies())
        {
            if (ButterflyRing > 0 && Game1.player.isWearingRing(ButterflyRing))
            {
                CRUtils.SpawnButterfly(critters, 3);
            }
        }
    }

    /// <inheritdoc cref="IGameLoopEvents.TimeChanged"/>
    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        Game1.currentLocation?.instantiateCrittersList();
        if (Game1.currentLocation?.critters is not List<Critter> critters)
        {
            return;
        }
        if (Game1.isDarkOut())
        {
            if (FireFlyRing > 0)
            {
                CRUtils.SpawnFirefly(critters, Game1.player.GetEffectsOfRingMultiplier(FireFlyRing));
            }
        }
        else if (Game1.currentLocation.ShouldSpawnButterflies())
        {
            if (ButterflyRing > 0)
            {
                CRUtils.SpawnButterfly(critters, Game1.player.GetEffectsOfRingMultiplier(ButterflyRing));
            }
        }
        if (BunnyRing > 0)
        {
            int delay = 0;
            foreach ((Vector2 position, bool flipped) in CRUtils.FindBunnySpawnTile(
                loc: Game1.currentLocation,
                playerTile: Game1.player.getTileLocation(),
                count: Game1.player.GetEffectsOfRingMultiplier(BunnyRing) * 2))
            {
                GameLocation location = Game1.currentLocation;
                DelayedAction.functionAfterDelay(
                () =>
                {
                    if (location == Game1.currentLocation)
                    {
                        CRUtils.SpawnRabbit(critters, position, location, flipped);
                    }
                },
                delay += Game1.random.Next(250, 750));
            }
        }
    }

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        MultiplayerHelpers.AssertMultiplayerVersions(this.Helper.Multiplayer, this.ModManifest, this.Monitor, this.Helper.Translation);

        this.migrator = new (this.ModManifest, this.Helper, this.Monitor);
        if (!this.migrator.CheckVersionInfo())
        {
            this.Helper.Events.GameLoop.Saved += this.WriteMigrationData;
        }
        else
        {
            this.migrator = null;
        }

        if (Context.IsMainPlayer)
        {
            // hook event to save Ids so future migrations are possible.
            this.Helper.Events.GameLoop.Saving -= this.OnSaving;
            this.Helper.Events.GameLoop.Saving += this.OnSaving;
        }
    }

    /// <summary>
    /// Resets the IDs when returning to the title.
    /// </summary>
    /// <param name="sender">SMAPI.</param>
    /// <param name="e">Event args.</param>
    [EventPriority(EventPriority.High)]
    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        bunnyRing = -1;
        butterflyRing = -1;
        fireflyRing = -1;
        owlRing = -1;
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            // handle patches from annotations.
            harmony.PatchAll(typeof(ModEntry).Assembly);
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }

        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    #region migration

    /// <inheritdoc cref="IGameLoopEvents.Saving"/>
    private void OnSaving(object? sender, SavingEventArgs e)
    {
        this.Helper.Events.GameLoop.Saving -= this.OnSaving;
        if (Context.IsMainPlayer)
        {
            DataModel data = this.Helper.Data.ReadSaveData<DataModel>(SAVEKEY) ?? new();
            bool changed = false;

            if (data.BunnyRing != BunnyRing)
            {
                data.BunnyRing = BunnyRing;
                changed = true;
            }

            if (data.ButterflyRing != ButterflyRing)
            {
                data.ButterflyRing = ButterflyRing;
                changed = true;
            }

            if (data.FireFlyRing != FireFlyRing)
            {
                data.FireFlyRing = FireFlyRing;
                changed = true;
            }

            if (data.OwlRing != OwlRing)
            {
                data.OwlRing = OwlRing;
                changed = true;
            }

            if (changed)
            {
                ModMonitor.Log("Writing ids into save.");
                this.Helper.Data.WriteSaveData("item_ids", data);
            }
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
