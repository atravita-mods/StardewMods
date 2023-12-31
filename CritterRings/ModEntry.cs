namespace CritterRings;

using AtraCore.Framework.Internal;
using AtraCore.Utilities;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.MigrationManager;
using AtraShared.Utils.Extensions;

using CritterRings.Framework;
using CritterRings.Framework.Managers;

using HarmonyLib;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

using StardewValley.BellsAndWhistles;
using StardewValley.Buffs;
using StardewValley.Locations;

using AtraUtils = AtraShared.Utils.Utils;

/// <inheritdoc />
[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Reviewed.")]
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <summary>
    /// A buff corresponding to the bunny ring.
    /// </summary>
    internal const string BunnyBuffId = "atravita.CritterRings_BunnyBuff";

    private MigrationManager? migrator;

    /// <summary>
    /// Gets the config instance for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <summary>
    /// Gets the API for CameraPan.
    /// </summary>
    internal static ICameraAPI? CameraAPI { get; private set; } = null;

    #region managers

    private static readonly PerScreen<BunnySpawnManager?> BunnyManagers = new(() => null);

    private static readonly PerScreen<JumpManager?> JumpManagers = new(() => null);

    /// <summary>
    /// Gets a reference to the current jumpManager, if applicable.
    /// </summary>
    internal static JumpManager? CurrentJumper => JumpManagers.Value;
    #endregion

    #region ItemConsts

    /// <summary>
    /// The unique ID of this mod.
    /// </summary>
    internal const string UniqueID = "atravita.CritterRings";

    /// <summary>
    /// The <see cref="Item.ItemId"/> of the bunny ring.
    /// </summary>
    internal const string BunnyRing = $"{UniqueID}_BunnyRing";

    /// <summary>
    /// The <see cref="Item.ItemId"/> of the butterfly ring.
    /// </summary>
    internal const string ButterflyRing = $"{UniqueID}_ButterflyRing";

    /// <summary>
    /// The <see cref="Item.ItemId"/> of the firefly ring.
    /// </summary>
    internal const string FireFlyRing = $"{UniqueID}_FireFlyRing";

    /// <summary>
    /// The <see cref="Item.ItemId"/> of the frog ring.
    /// </summary>
    internal const string FrogRing = $"{UniqueID}_FrogRing";

    /// <summary>
    /// The <see cref="Item.ItemId"/> of the owl ring.
    /// </summary>
    internal const string OwlRing = $"{UniqueID}_OwlRing";

    #endregion

    #region initialization

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        base.Entry(helper);
        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
        AssetManager.Initialize(helper.GameContent);
    }

    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.Helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        this.Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        this.Helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        this.Helper.Events.Player.Warped += this.OnWarp;
        this.Helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;

        this.Helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Apply(e);
        this.Helper.Events.Content.AssetsInvalidated += static (_, e) => AssetManager.Reset(e.NamesWithoutLocale);

        JumpManager.Initialize(this.Helper.ModRegistry);

        GMCMHelper gmcmHelper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (gmcmHelper.TryGetAPI())
        {
            gmcmHelper.Register(
                reset: static () => Config = new(),
                save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config))
            .AddParagraph(I18n.Mod_Description)
            .GenerateDefaultGMCM(static () => Config);
        }

        {
            IntegrationHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, LogLevel.Trace);
            if (helper.TryGetAPI("atravita.CameraPan", "0.1.1", out ICameraAPI? api))
            {
                CameraAPI = api;
            }
        }
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

    #endregion

    /// <inheritdoc cref="IInputEvents.ButtonsChanged"/>
    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsPlayerFree)
        {
            return;
        }

        // Frog ring.
        if (!Game1.player.UsingTool && !Game1.player.isEating && Game1.player.yJumpOffset == 0
            && Config.MaxFrogJumpDistance > 0 && Config.FrogRingButton.Keybinds.FirstOrDefault(k => k.GetState() == SButtonState.Pressed) is Keybind keybind
            && Game1.player.isWearingRing(FrogRing))
        {
            if (JumpManagers.Value?.IsValid(out _) == true)
            {
                ModMonitor.Log($"Jump already in progress for this player, skipping.");
            }
            else if (Game1.player.isRidingHorse())
            {
                Game1.showRedMessage(I18n.FrogRing_Horse());
            }
            else if (Game1.player.exhausted.Value || (Game1.player.Stamina < Config.MaxFrogJumpDistance && Config.JumpCostsStamina))
            {
                Game1.showRedMessage(I18n.BunnyBuff_Tired());
            }
            else
            {
                JumpManagers.Value?.Dispose();
                JumpManagers.Value = new(Game1.player, this.Helper.Events.GameLoop, this.Helper.Events.Display, keybind);
            }
        }

        // Bunny ring.
        if (Config.BunnyRingBoost > 0 && Config.BunnyRingButton.JustPressed()
            && Game1.player.isWearingRing(BunnyRing) && !Game1.player.hasBuff(BunnyBuffId))
        {
            if (Game1.player.Stamina >= Config.BunnyRingStamina && !Game1.player.exhausted.Value)
            {
                Buff buff = new(
                    id: BunnyBuffId,
                    displayName: I18n.BunnyRing_Name(),
                    description: I18n.BunnyBuff_Description(Config.BunnyRingBoost),
                    iconTexture: AssetManager.BuffTexture,
                    iconSheetIndex: 1,
                    duration: 20 * Game1.realMilliSecondsPerGameMinute, // 20 in game minutes
                    effects: new BuffEffects() { Speed = { Config.BunnyRingBoost } });
                Game1.player.applyBuff(buff);

                Game1.player.Stamina -= Config.BunnyRingStamina;
            }
            else
            {
                Game1.showRedMessage(I18n.BunnyBuff_Tired());
            }
        }
    }

    #region critter spawning

    /// <inheritdoc cref="IPlayerEvents.Warped"/>
    [EventPriority(EventPriority.Low)]
    private void OnWarp(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer || ReferenceEquals(e.OldLocation, e.NewLocation))
        {
            return;
        }

        // forcibly end the jump if the player was in one.
        JumpManagers.Value?.Dispose();
        JumpManagers.Value = null;

        if (Config.CritterSpawnMultiplier == 0)
        {
            return;
        }

        e.NewLocation?.instantiateCrittersList();
        if (e.NewLocation?.critters is not List<Critter> critters)
        {
            return;
        }
        if (Game1.isDarkOut(e.NewLocation))
        {
            if (Game1.player.isWearingRing(FireFlyRing))
            {
                CRUtils.SpawnFirefly(critters, 5);
            }
        }
        else if (e.NewLocation.ShouldSpawnButterflies() && Game1.player.isWearingRing(ButterflyRing))
        {
            CRUtils.SpawnButterfly(e.NewLocation, critters, 3);
        }
        if (e.NewLocation is not Caldera && Game1.player.isWearingRing(BunnyRing))
        {
            if (BunnyManagers.Value?.IsValid() == false)
            {
                BunnyManagers.Value.Dispose();
                BunnyManagers.Value = null;
            }
            BunnyManagers.Value ??= new(this.Monitor, Game1.player, this.Helper.Events.Player);
            CRUtils.AddBunnies(critters, 5, BunnyManagers.Value.GetTrackedBushes(), e.NewLocation);
        }
        if (Game1.player.isWearingRing(FrogRing) && e.NewLocation.ShouldSpawnFrogs())
        {
            CRUtils.SpawnFrogs(e.NewLocation, critters, 5);
        }

        if (Game1.player.isWearingRing(OwlRing) && e.NewLocation.ShouldSpawnOwls())
        {
            CRUtils.SpawnOwls(e.NewLocation, critters, 1);
        }
    }

    /// <inheritdoc cref="IGameLoopEvents.TimeChanged"/>
    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (Config.CritterSpawnMultiplier == 0)
        {
            return;
        }
        Game1.currentLocation?.instantiateCrittersList();
        if (Game1.currentLocation?.critters is not List<Critter> critters)
        {
            return;
        }
        if (Game1.isDarkOut(Game1.currentLocation))
        {
            CRUtils.SpawnFirefly(critters, Game1.player.GetEffectsOfRingMultiplier(FireFlyRing));
        }
        else if (Game1.currentLocation.ShouldSpawnButterflies())
        {
            CRUtils.SpawnButterfly(Game1.currentLocation, critters, Game1.player.GetEffectsOfRingMultiplier(ButterflyRing));
        }
        if (Game1.currentLocation.ShouldSpawnFrogs())
        {
            CRUtils.SpawnFrogs(Game1.currentLocation, critters, Game1.player.GetEffectsOfRingMultiplier(FrogRing));
        }

        if (Game1.player.isWearingRing(OwlRing) && Game1.currentLocation.ShouldSpawnOwls())
        {
            CRUtils.SpawnOwls(Game1.currentLocation, critters, Game1.player.GetEffectsOfRingMultiplier(OwlRing));
        }

        if (Game1.currentLocation is not Caldera)
        {
            if (BunnyManagers.Value?.IsValid() == false)
            {
                BunnyManagers.Value.Dispose();
                BunnyManagers.Value = null;
            }
            BunnyManagers.Value ??= new(this.Monitor, Game1.player, this.Helper.Events.Player);
            CRUtils.AddBunnies(critters, Game1.player.GetEffectsOfRingMultiplier(BunnyRing), BunnyManagers.Value.GetTrackedBushes());
        }
    }

    #endregion

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
    }

    /// <summary>
    /// Resets the IDs when returning to the title.
    /// </summary>
    /// <param name="sender">SMAPI.</param>
    /// <param name="e">Event args.</param>
    [EventPriority(EventPriority.High)]
    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        // reset and yeet managers.
        foreach ((_, JumpManager? value) in JumpManagers.GetActiveValues())
        {
            value?.Dispose();
        }
        JumpManagers.ResetAllScreens();
        foreach ((_, BunnySpawnManager? value) in BunnyManagers.GetActiveValues())
        {
            value?.Dispose();
        }
        BunnyManagers.ResetAllScreens();
    }

    #region migration

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
