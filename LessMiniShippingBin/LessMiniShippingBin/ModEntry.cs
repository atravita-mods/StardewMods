
using HarmonyLib;

using MiniAtraShared.Extensions;
using MiniAtraShared.Models;

using StardewModdingAPI.Events;

using AtraUtils = MiniAtraShared.Utils;

namespace LessMiniShippingBin;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <summary>
    /// Gets or sets an instance of the configuration class for this mod.
    /// </summary>
    internal static ModConfig Config { get; set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        base.Entry(helper);
        I18n.Init(helper.Translation);
        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);
        ApplyPatches(new Harmony(this.ModManifest.UniqueID));
        helper.Events.GameLoop.GameLaunched += this.SetUpConfig;
    }

    /// <summary>
    /// Applies and logs this mod's harmony patches.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    private static void ApplyPatches(Harmony harmony) => harmony.PatchAll(typeof(ModEntry).Assembly);

    /// <summary>
    /// Generates the GMCM for this mod.
    /// </summary>
    /// <param name="sender">SMAPI.</param>
    /// <param name="e">Arguments for event.</param>
    private void SetUpConfig(object? sender, GameLaunchedEventArgs e)
    {
        const string GMCM = "spacechase0.GenericModConfigMenu";
        if (this.Helper.ModRegistry.Get(GMCM) is not { } gmcm || gmcm.Manifest.Version.IsOlderThan("1.9.0"))
        {
            return;
        }

        var api = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api is null)
        {
            return;
        }

        api.Register(
            this.ModManifest,
            reset: static () => Config = new(),
            save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config));

        api.AddParagraph(
            this.ModManifest,
            I18n.Mod_Description);

        api.AddNumberOption(
            this.ModManifest,
            static () => Config.MiniShippingCapacity,
            static value => Config.MiniShippingCapacity = value,
            I18n.MiniShippingCapacity_Title,
            I18n.MiniShippingCapacity_Description,
            min: ModConfig.MIN_CHEST_SIZE,
            max: ModConfig.MAX_CHEST_SIZE,
            interval: 1);

        api.AddNumberOption(
            this.ModManifest,
            static () => Config.JuminoCapacity,
            static value => Config.JuminoCapacity = value,
            I18n.JuminoCapacity_Title,
            I18n.JuminoCapacity_Description,
            min: ModConfig.MIN_CHEST_SIZE,
            max: ModConfig.MAX_CHEST_SIZE,
            interval: 1);

        api.AddBoolOption(
            this.ModManifest,
            static () => Config.DrawFirstItem,
            static value => Config.DrawFirstItem = value,
            I18n.DrawFirstItem_Title,
            I18n.DrawFirstItem_Description
            );

        api.AddBoolOption(
            this.ModManifest,
            static () => Config.DrawFirstItemFridge,
            static value => Config.DrawFirstItemFridge = value,
            I18n.DrawFirstItemFridge_Title,
            I18n.DrawFirstItemFridge_Description
            );
    }
}
