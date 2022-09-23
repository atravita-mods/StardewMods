using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using StardewModdingAPI.Events;
using AtraUtils = AtraShared.Utils.Utils;

namespace SingleParenthood;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    internal const string CountUp = "atravita.SingleParenthood.CountUp";
    internal const string Type = "atravita.SingleParenthood.Type";
    internal const string Relationship = "atravita.SingleParenthood.Relationship";

    private static IPregnancyRoleApi? pregancyRoleApi;

    internal static IPregnancyRoleApi? PregnancyRoleApi => pregancyRoleApi;

    internal static IMonitor ModMonitor { get; private set; } = null!;

    internal static IInputHelper InputHelper { get; private set; } = null!;

    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModMonitor = this.Monitor;
        InputHelper = this.Helper.Input;
        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunch;
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
            this.Monitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }

        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    private void OnGameLaunch(object? sender, GameLaunchedEventArgs e)
    {
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

        IntegrationHelper integrationHelper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, LogLevel.Debug);
        _ = integrationHelper.TryGetAPI("kdau.PregnancyRole", "2.0.0", out pregancyRoleApi);

        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (helper.TryGetAPI())
        {
            helper.Register(
                reset: static () => Config = new(),
                save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config))
            .GenerateDefaultGMCM(static () => Config);
        }
    }
}
