using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraCore.Framework.Internal;
using AtraCore.Utilities;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.MigrationManager;
using AtraShared.Utils.Extensions;

using GiantCropFertilizer.HarmonyPatches;

using HarmonyLib;

using StardewModdingAPI.Events;

using AtraUtils = AtraShared.Utils.Utils;

namespace GiantCropFertilizer;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <summary>
    /// The <see cref="Item.ItemId"/> of the giant crop fertilizer.
    /// </summary>
    internal const string GiantCropFertilizerID = "atravita.GiantCropFertilizer";

    /// <summary>
    /// The <see cref="Item.QualifiedItemId" /> of the giant crop fertilizer.
    /// </summary>
    internal const string QualifiedGiantCropFertilizerID = $"{ItemRegistry.type_object}{GiantCropFertilizerID}";

    private MigrationManager? migrator;

    /// <summary>
    /// Gets the config instance for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        base.Entry(helper);

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        AssetManager.Init(helper.GameContent);
    }

    /// <summary>
    /// Checks to see if a fertilizer string matches the giant crop fertilizer.
    /// </summary>
    /// <param name="fertilizer">Fertilizer to check.</param>
    /// <returns>True if matches, false otherwise.</returns>
    [MethodImpl(TKConstants.Hot)]
    internal static bool IsGiantCropFertilizer(string? fertilizer)
        => fertilizer is GiantCropFertilizerID or QualifiedGiantCropFertilizerID;

    /// <summary>
    /// Applies the patches for this mod.
    /// </summary>
    /// <param name="harmony">This mod's harmony instance.</param>
    /// <remarks>Delay until GameLaunched in order to patch other mods....</remarks>
    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll(typeof(ModEntry).Assembly);
            HoeDirtDrawTranspiler.ApplyPatches(harmony);
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;

        this.Helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Apply(e);

        // GMCM integration
        {
            GMCMHelper gmcmHelper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
            if (gmcmHelper.TryGetAPI())
            {
                gmcmHelper.Register(
                    reset: static () => Config = new(),
                    save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config))
                .AddParagraph(I18n.ModDescription)
                .AddNumberOption(
                    name: I18n.GiantCropChance_Title,
                    getValue: static () => (float)Config.GiantCropChance,
                    setValue: static (float val) => Config.GiantCropChance = val,
                    tooltip: I18n.GiantCropChance_Description,
                    min: 0f,
                    max: 1.1f,
                    interval: 0.01f);

                if (this.Helper.ModRegistry.IsLoaded("spacechase0.MoreGiantCrops"))
                {
                    gmcmHelper.AddParagraph(I18n.AllowGiantCropsParagraph);
                }
                else
                {
                    gmcmHelper.AddBoolOption(
                        name: I18n.AllowGiantCropsOffFarm_Title,
                        getValue: static () => Config.AllowGiantCropsOffFarm,
                        setValue: static (val) => Config.AllowGiantCropsOffFarm = val,
                        tooltip: I18n.AllowGiantCropsOffFarm_Description);
                }
            }
        }

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsSplitScreen && Context.ScreenId != 0)
        {
            return;
        }
        MultiplayerHelpers.AssertMultiplayerVersions(this.Helper.Multiplayer, this.ModManifest, this.Monitor, this.Helper.Translation);
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
}
