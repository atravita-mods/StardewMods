using System.Reflection;
using AtraShared.Integrations;
using AtraShared.MigrationManager;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StopRugRemoval.HarmonyPatches.BombHandling;

namespace StopRugRemoval;

/// <summary>
/// Entry class to the mod.
/// </summary>
public class ModEntry : Mod
{
    private MigrationManager? migrator;

    private static readonly Lazy<IReflectedField<Multiplayer>> multiplayer = new(() => ReflectionHelper!.GetField<Multiplayer>(typeof(Game1), "multiplayer"));

    /// <summary>
    /// Gets Game1.multiplayer.
    /// </summary>
    /// <remarks>This still requires reflection and is likely slow.</remarks>
    internal static Multiplayer Multiplayer => multiplayer.Value.GetValue();

    // the following two properties are set in the entry method, which is approximately as close as I can get to the constructor anyways.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Gets the logger for this file.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; }

    /// <summary>
    /// Gets instance that holds the configuration for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; }

    internal static IReflectionHelper ReflectionHelper { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModMonitor = this.Monitor;
        ReflectionHelper = this.Helper.Reflection;
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
        helper.Events.GameLoop.SaveLoaded += this.SaveLoaded;
        helper.Events.Player.Warped += this.Player_Warped;

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    private void Player_Warped(object? sender, WarpedEventArgs e)
        => ConfirmBomb.HaveConfirmed.Value = false;

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

    /// <summary>
    /// Raised when save is loaded.
    /// </summary>
    /// <param name="sender">Unknown, used by SMAPI.</param>
    /// <param name="e">Parameters.</param>
    private void SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsSplitScreen && Context.ScreenId != 0)
        {
            return;
        }
        this.migrator = new(this.ModManifest, this.Helper, this.Monitor);
        this.migrator.ReadVersionInfo();

        this.Helper.Events.GameLoop.Saved += this.WriteMigrationData;
    }

    /// <summary>
    /// Writes migration data then detaches the migrator.
    /// </summary>
    /// <param name="sender">Smapi thing.</param>
    /// <param name="e">Arguments for just-before-saving.</param>
    private void WriteMigrationData(object? sender, SavedEventArgs e)
    {
        if (this.migrator is not null)
        {
            this.migrator.SaveVersionInfo();
            this.migrator = null;
        }
        this.Helper.Events.GameLoop.Saved -= this.WriteMigrationData;
    }
}
