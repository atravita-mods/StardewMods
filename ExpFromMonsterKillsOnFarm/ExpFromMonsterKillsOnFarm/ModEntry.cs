using AtraShared.Integrations;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using StardewModdingAPI.Events;

using AtraUtils = AtraShared.Utils.Utils;

namespace ExpFromMonsterKillsOnFarm;

/// <inheritdoc />
internal sealed class ModEntry : Mod
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
        harmony.Snitch(this.Monitor, this.ModManifest.UniqueID, transpilersOnly: true);
    }

    /// <summary>
    /// Generates the GMCM for this mod by looking at the structure of the config class.
    /// </summary>
    /// <param name="sender">Unknown, expected by SMAPI.</param>
    /// <param name="e">Arguments for eevnt.</param>
    /// <remarks>To add a new setting, add the details to the i18n file. Currently handles: bool.</remarks>
    private void SetUpConfig(object? sender, GameLaunchedEventArgs e)
    {
        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (helper.TryGetAPI())
        {
            helper.Register(
                reset: static () => Config = new ModConfig(),
                save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config))
            .AddParagraph(I18n.Mod_Description)
            .GenerateDefaultGMCM(static () => Config);
        }
    }
}
