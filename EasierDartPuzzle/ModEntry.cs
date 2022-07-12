using System.Reflection;
using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using StardewModdingAPI.Events;
using AtraUtils = AtraShared.Utils.Utils;

namespace EasierDartPuzzle;

/// <inheritdoc/>
internal class ModEntry : Mod
{
    /// <summary>
    /// Gets the logger for this file.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;
        I18n.Init(helper.Translation);
        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunch;
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    private static ModConfig GetConfig() => Config;

    private void OnGameLaunch(object? sender, GameLaunchedEventArgs e)
    {
        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (helper.TryGetAPI())
        {
            helper.Register(
                reset: static () => Config = new(),
                save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config));

            foreach (PropertyInfo prop in typeof(ModConfig).GetProperties())
            {
                if (prop.PropertyType == typeof(int))
                {
                    helper.AddIntOption(
                        property: prop,
                        getConfig: GetConfig,
                        min: prop.Name.EndsWith("Count") ? 8 : 600,
                        max: prop.Name.EndsWith("Count") ? 30 : 2000);
                }
                else if (prop.PropertyType == typeof(bool))
                {
                    helper.AddBoolOption(prop, GetConfig);
                }
                else if (prop.PropertyType == typeof(float))
                {
                    helper.AddFloatOption(
                        property: prop,
                        getConfig: GetConfig,
                        min: 0.05f,
                        max: 20f);
                }
            }
        }
    }
}