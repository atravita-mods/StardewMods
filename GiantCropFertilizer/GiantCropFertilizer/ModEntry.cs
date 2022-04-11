using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;
using GiantCropFertilizer.HarmonyPatches;
using HarmonyLib;
using StardewModdingAPI.Events;

namespace GiantCropFertilizer;

/// <inheritdoc />
internal class ModEntry : Mod
{
    private static IJsonAssetsAPI? jsonAssets;

    private int countdown = 5;

    /// <summary>
    /// Gets the integer ID of the giant crop fertilizer. -1 if not found/not loaded yet.
    /// </summary>
    internal static int GiantCropFertilizerID => jsonAssets?.GetObjectId("Giant Crop Fertilizer") ?? -1;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModMonitor = this.Monitor;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
    }

    /// <summary>
    /// Applies the patches for this mod.
    /// </summary>
    /// <param name="harmony">This mod's harmony instance.</param>
    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            Harmony.DEBUG = true;
            harmony.PatchAll();
            if (this.Helper.ModRegistry.IsLoaded("spacechase0.MultiFertilizer"))
            {
                HoeDirtPatcher.ApplyPatches(harmony);
                MultiFertilizerDrawTranspiler.ApplyPatches(harmony);
            }
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: false);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

        IntegrationHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, LogLevel.Warn);
        if (helper.TryGetAPI("spacechase0.JsonAssets", "1.10.3", out jsonAssets))
        {
            jsonAssets.LoadAssets(Path.Combine(this.Helper.DirectoryPath, "assets", "json-assets"), this.Helper.Translation);
        }

        this.Helper.Events.GameLoop.UpdateTicked += this.FiveTicksPostGameLaunched;
    }

    private void FiveTicksPostGameLaunched(object? sender, UpdateTickedEventArgs e)
    {
        if (--this.countdown <= 0)
        {
            this.Helper.Content.AssetEditors.Add(AssetEditor.Instance);
            this.Helper.Events.GameLoop.UpdateTicked -= this.FiveTicksPostGameLaunched;
        }
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        MultiplayerHelpers.AssertMultiplayerVersions(this.Helper.Multiplayer, this.ModManifest, this.Monitor, this.Helper.Translation);
    }
}