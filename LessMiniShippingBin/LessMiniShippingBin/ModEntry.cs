using AtraShared.Integrations;
using HarmonyLib;
using StardewModdingAPI.Events;
using AtraUtils = AtraShared.Utils.Utils;

namespace LessMiniShippingBin;

/// <inheritdoc />
public class ModEntry : Mod
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    // ModMonitor is set in Entry, which is as close I can reasonaby get to the constructor.

    /// <summary>
    /// Gets logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; }

    /// <summary>
    /// Gets or sets an instance of the configuration class for this mod.
    /// </summary>
    internal static ModConfig Config { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;
        I18n.Init(helper.Translation);
        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
        helper.Events.GameLoop.GameLaunched += this.SetUpConfig;
    }

    /// <summary>
    /// Applies and logs this mod's harmony patches.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    private void ApplyPatches(Harmony harmony)
    {
        // handle patches from annotations.
        harmony.PatchAll();
    }

    /// <summary>
    /// Generates the GMCM for this mod.
    /// </summary>
    /// <param name="sender">SMAPI</param>
    /// <param name="e">Arguments for event.</param>
    private void SetUpConfig(object? sender, GameLaunchedEventArgs e)
    {
        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (!helper.TryGetAPI())
        {
            return;
        }

        helper.Register(
                reset: static () => Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(Config))
            .AddParagraph(I18n.Mod_Description)
            .AddNumberOption(
                getValue: static () => Config.MiniShippingCapacity,
                setValue: static value => Config.MiniShippingCapacity = value,
                name: I18n.Config_Capacity_Title,
                tooltip: I18n.Config_Capacity_Description,
                min: 9,
                max: 48,
                interval: 9)
            .AddNumberOption(
                getValue: static () => Config.JuminoCapcaity,
                setValue: static value => Config.JuminoCapcaity = value,
                name: I18n.Config_Junimo_Title,
                tooltip: I18n.Config_Junimo_Description,
                min: 9,
                max: 48,
                interval: 9);
    }
}
