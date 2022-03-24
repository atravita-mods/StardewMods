using System.Reflection;
using AtraShared.Integrations;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using StardewModdingAPI.Events;

namespace TrashDoesNotConsumeBait;

/// <inheritdoc/>
internal class ModEntry : Mod
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; }

    /// <summary>
    /// Gets or sets the config instance for this mod.
    /// </summary>
    internal static ModConfig Config { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;
        I18n.Init(helper.Translation);

        try
        {
            Config = this.Helper.ReadConfig<ModConfig>();
        }
        catch
        {
            this.Monitor.Log(I18n.IllFormatedConfig(), LogLevel.Warn);
            Config = new();
        }

        helper.Events.GameLoop.GameLaunched += this.SetUpConfig;
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    private void SetUpConfig(object? sender, GameLaunchedEventArgs e)
    {
        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if(!helper.TryGetAPI())
        {
            return;
        }
        helper.Register(
            reset: () => Config = new(),
            save: () => this.Helper.WriteConfig(Config));
        foreach (PropertyInfo property in typeof(ModConfig).GetProperties())
        {

            if (property.PropertyType == typeof(bool))
            {
                helper.AddBoolOption(
                    property: property,
                    getConfig: () => Config);
            }
        }
        helper.AddPageHere("CheatyStuff", I18n.CheatyStuffTitle);
        foreach (PropertyInfo property in typeof(ModConfig).GetProperties())
        {
            if (property.PropertyType == typeof(float))
            {
                helper.AddFloatOption(
                    property: property,
                    getConfig: () => Config,
                    min: 0f,
                    max: 1f,
                    interval: 0.01f);
            }
        }
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            ModMonitor.Log($"Mod crashed while applying harmony patches:\n\n{ex}", LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, this.ModManifest.UniqueID);
    }
}