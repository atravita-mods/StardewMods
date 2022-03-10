using System.Reflection;
using AtraShared.Integrations;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace StopRugRemoval;

/// <summary>
/// Entry class to the mod.
/// </summary>
public class ModEntry : Mod
{
    // the following two fields are set in the entry method, which is approximately as close as I can get to the constructor anyways.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Gets the logger for this file.
    /// </summary>
    public static IMonitor ModMonitor { get; private set; }

    /// <summary>
    /// Gets instance that holds the configuration for this mod.
    /// </summary>
    public static ModConfig Config { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModMonitor = this.Monitor;
        try
        {
            Config = this.Helper.ReadConfig<ModConfig>();
        }
        catch
        {
            this.Monitor.Log(I18n.IllFormatedConfig(), LogLevel.Warn);
            Config = new();
        }

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

        helper.Events.GameLoop.GameLaunched += this.SetUpConfig;
        //helper.Events.GameLoop.Saving += this.BeforeSave;
        //saved as well?
    }

    /// <summary>
    /// Applies and logs this mod's harmony patches.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            // handle patches from annotations.
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            ModMonitor.Log($"Mod crashed while applying harmony patches\n\n{ex}", LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, this.ModManifest.UniqueID);
    }

    /// <summary>
    /// Clear all NoSpawn tiles before saving.
    /// </summary>
    /// <param name="sender">From SMAPI.</param>
    /// <param name="e">Saving Event arguments...</param>
    /// <exception cref="NotImplementedException">Haven't finished writing this yet.</exception>
    private void BeforeSave(object? sender, SavingEventArgs e) => throw new NotImplementedException();

    private void SetUpConfig(object? sender, GameLaunchedEventArgs e)
    {
        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (!helper.TryGetAPI())
        {
            return;
        }

        helper.Register(
                reset: () => Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(Config))
            .AddParagraph(I18n.Mod_Description);

        foreach (PropertyInfo property in typeof(ModConfig).GetProperties())
        {
            if (property.PropertyType == typeof(bool))
            {
                helper.AddBoolOption(property, () => Config);
            }
            else if (property.PropertyType == typeof(KeybindList))
            {
                helper.AddKeybindList(property, () => Config);
            }
        }
    }
}
