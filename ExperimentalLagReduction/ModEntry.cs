// Ignore Spelling: Api

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Utils.Extensions;

using ExperimentalLagReduction.Framework;
using ExperimentalLagReduction.HarmonyPatches;

using HarmonyLib;
using StardewModdingAPI.Events;

using AtraUtils = AtraShared.Utils.Utils;

namespace ExperimentalLagReduction;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <summary>
    /// Gets the config instance for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // initialize translations.
        I18n.Init(helper.Translation);

        // statics
        ModMonitor = this.Monitor;
        this.Monitor.Log($"Starting up: {this.ModManifest.UniqueID} - {typeof(ModEntry).Assembly.FullName}");
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunch;

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        OverrideGiftTastes.Initialize(helper.GameContent);
        ConsoleCommandManager.Register(helper.ConsoleCommands);

        AssetManager.Initialize(helper.GameContent);
        helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Apply(e);
    }

    /// <inheritdoc />
    public override object? GetApi() => new API();

    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    private void OnGameLaunch(object? sender, GameLaunchedEventArgs e)
    {
        this.Helper.Events.Content.AssetsInvalidated += (_, e) => OverrideGiftTastes.Reset(e.NamesWithoutLocale);
        this.Helper.Events.GameLoop.DayEnding += (_, _) => OverrideGiftTastes.Reset();

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

        // GMCM
        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (helper.TryGetAPI())
        {
            helper.Register(
                reset: () => Config = new(),
                save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config))
            .GenerateDefaultGMCM(() => Config);
        }
    }

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
            RedirectToLazyLoad.ApplyPatches(new Harmony(harmony.Id + "_lazy"));
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }
}
