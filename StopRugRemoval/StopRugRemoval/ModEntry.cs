using System.Reflection;
using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.MigrationManager;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StopRugRemoval.Configuration;
using StopRugRemoval.HarmonyPatches;
using StopRugRemoval.HarmonyPatches.Confirmations;
using StopRugRemoval.HarmonyPatches.Niceties;

using AtraUtils = AtraShared.Utils.Utils;

namespace StopRugRemoval;

/// <summary>
/// Entry class to the mod.
/// </summary>
public class ModEntry : Mod
{
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1306:Field names should begin with lower-case letter", Justification = "Reviewed.")]
    private static GMCMHelper? GMCM = null;

    private MigrationManager? migrator;

    /// <summary>
    /// Gets a function that gets Game1.multiplayer.
    /// </summary>
    internal static Func<Multiplayer> Multiplayer => MultiplayerHelpers.GetMultiplayer;

    // the following three properties are set in the entry method, which is approximately as close as I can get to the constructor anyways.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Gets the logger for this file.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; }

    /// <summary>
    /// Gets instance that holds the configuration for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; }

    /// <summary>
    /// Gets the reflection helper for this mod.
    /// </summary>
    internal static IReflectionHelper ReflectionHelper { get; private set; }

    /// <summary>
    /// Gets the game content helper for this mod.
    /// </summary>
    internal static IGameContentHelper GameContentHelper { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModMonitor = this.Monitor;
        ReflectionHelper = this.Helper.Reflection;
        GameContentHelper = this.Helper.GameContent;
        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunch;
        helper.Events.GameLoop.SaveLoaded += this.SaveLoaded;
        helper.Events.Player.Warped += this.Player_Warped;

        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.Content.AssetsInvalidated += this.OnAssetInvalidated;
        helper.Events.Content.LocaleChanged += this.OnLocaleChange;

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    private void OnLocaleChange(object? sender, LocaleChangedEventArgs e)
        => AssetEditor.Refresh();

    private void OnAssetInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        => AssetEditor.Refresh(e.NamesWithoutLocale);

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        => AssetEditor.Edit(e, this.Helper.ModRegistry, this.Helper.DirectoryPath);

    private void Player_Warped(object? sender, WarpedEventArgs e)
    {
        SObjectPatches.HaveConfirmedBomb.Value = false;
        ConfirmWarp.HaveConfirmed.Value = false;
    }

    /// <summary>
    /// Applies and logs this mod's harmony patches.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            // handle patches from annotations.
            harmony.PatchAll();

            if (!this.Helper.ModRegistry.IsLoaded("DecidedlyHuman.BetterReturnScepter"))
            {
                ConfirmWarp.ApplyWandPatches(harmony);
            }
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    /// <summary>
    /// Applies the patches that must be applied after all mods are initialized.
    /// IE - patches on other mods.
    /// </summary>
    /// <param name="harmony">A harmony instance.</param>
    private void ApplyLatePatches(Harmony harmony)
    {
        try
        {
            FruitTreesAvoidHoe.ApplyPatches(harmony, this.Helper.ModRegistry);
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    private void OnGameLaunch(object? sender, GameLaunchedEventArgs e)
    {
        PlantGrassUnder.GetSmartBuildingBuildMode(this.Helper.ModRegistry);
        this.ApplyLatePatches(new Harmony(this.ModManifest.UniqueID + "+latepatches"));

        GMCM = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (!GMCM.TryGetAPI())
        {
            return;
        }
        this.SetUpBasicConfig();
    }

    /// <summary>
    /// Raised when save is loaded.
    /// </summary>
    /// <param name="sender">Unknown, used by SMAPI.</param>
    /// <param name="e">Parameters.</param>
    private void SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsSplitScreen && Context.ScreenId != 0)
        {
            return;
        }

        // Have to wait until here to populate locations
        Config.PrePopulateLocations();
        this.Helper.WriteConfig(Config);

        this.migrator = new(this.ModManifest, this.Helper, this.Monitor);
        this.migrator.ReadVersionInfo();

        this.Helper.Events.GameLoop.Saved += this.WriteMigrationData;

        if (GMCM?.HasGottenAPI == true)
        {
            GMCM.Unregister();
            this.SetUpBasicConfig();
            GMCM.AddPageHere("Bombs", I18n.BombLocationDetailed)
                .AddParagraph(I18n.BombLocationDetailed_Description);

            foreach (GameLocation loc in Game1.locations)
            {
                GMCM.AddEnumOption(
                    name: () => loc.NameOrUniqueName,
                    getValue: () => Config.SafeLocationMap.TryGetValue(loc.NameOrUniqueName, out IsSafeLocationEnum val) ? val : IsSafeLocationEnum.Dynamic,
                    setValue: (value) => Config.SafeLocationMap[loc.NameOrUniqueName] = value);
            }
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

    private void SetUpBasicConfig()
    {
        GMCM!.Register(
                reset: () =>
                {
                    Config = new ModConfig();
                    Config.PrePopulateLocations();
                },
                save: () => this.Helper.WriteConfig(Config))
            .AddParagraph(I18n.Mod_Description);

        foreach (PropertyInfo property in typeof(ModConfig).GetProperties())
        {
            if (property.PropertyType == typeof(bool))
            {
                GMCM.AddBoolOption(property, () => Config);
            }
            else if (property.PropertyType == typeof(KeybindList))
            {
                GMCM.AddKeybindList(property, () => Config);
            }
        }

        GMCM!.AddSectionTitle(I18n.ConfirmWarps_Title)
            .AddParagraph(I18n.ConfirmWarps_Description)
            .AddEnumOption(
                name: I18n.WarpsInSafeAreas_Title,
                getValue: () => Config.WarpsInSafeAreas,
                setValue: (value) => Config.WarpsInSafeAreas = value,
                tooltip: I18n.WarpsInSafeAreas_Description)
            .AddEnumOption(
                name: I18n.WarpsInDangerousAreas_Title,
                getValue: () => Config.WarpsInDangerousAreas,
                setValue: (value) => Config.WarpsInDangerousAreas = value,
                tooltip: I18n.WarpsInDangerousAreas_Description);
        GMCM!.AddSectionTitle(I18n.ConfirmBomb_Title)
            .AddParagraph(I18n.ConfirmBomb_Description)
            .AddEnumOption(
                name: I18n.BombsInSafeAreas_Title,
                getValue: () => Config.BombsInSafeAreas,
                setValue: (value) => Config.BombsInSafeAreas = value,
                tooltip: I18n.BombsInSafeAreas_Description)
            .AddEnumOption(
                name: I18n.BombsInDangerousAreas_Title,
                getValue: () => Config.BombsInDangerousAreas,
                setValue: (value) => Config.BombsInDangerousAreas = value,
                tooltip: I18n.BombsInDangerousAreas_Description);
    }
}
