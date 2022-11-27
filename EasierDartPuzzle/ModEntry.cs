using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using StardewModdingAPI.Events;
using AtraUtils = AtraShared.Utils.Utils;

namespace EasierDartPuzzle;

/// <inheritdoc/>
internal sealed class ModEntry : Mod
{
    /// <summary>
    /// Gets the logger for this file.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <summary>
    /// Gets the config instance for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;
        I18n.Init(helper.Translation);
        AssetManager.Initialize(helper.GameContent);
        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);
        if (Config.MinDartCount > Config.MaxDartCount)
        {
            (Config.MinDartCount, Config.MaxDartCount) = (Config.MaxDartCount, Config.MinDartCount);
            helper.AsyncWriteConfig(this.Monitor, Config);
        }

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunch;

        helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Apply(e);
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll(typeof(ModEntry).Assembly);
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    private void OnGameLaunch(object? sender, GameLaunchedEventArgs e)
    {
        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (helper.TryGetAPI())
        {
            helper.Register(
                reset: static () => Config = new(),
                save: () =>
                {
                    if (Config.MinDartCount > Config.MaxDartCount)
                    {
                        (Config.MinDartCount, Config.MaxDartCount) = (Config.MaxDartCount, Config.MinDartCount);
                    }
                    this.Helper.AsyncWriteConfig(this.Monitor, Config);
                })
            .AddParagraph(I18n.ModDescription)
            .GenerateDefaultGMCM(static () => Config);
        }
    }
}