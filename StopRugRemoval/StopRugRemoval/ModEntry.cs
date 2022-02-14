using System.Reflection;
using System.Text;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley.Objects;

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
        try
        {
            Config = this.Helper.ReadConfig<ModConfig>();
        }
        catch
        {
            this.Monitor.Log(I18n.IllFormatedConfig(), LogLevel.Warn);
            Config = new();
        }

        ModMonitor = this.Monitor;
        I18n.Init(helper.Translation);

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
        // handle patches from annotations.
        harmony.PatchAll();
        foreach (MethodBase? method in harmony.GetPatchedMethods())
        {
            if (method is null)
            {
                continue;
            }
            Patches patches = Harmony.GetPatchInfo(method);

            StringBuilder sb = new();
            sb.Append("Patched method ").Append(method.GetFullName());
            foreach (Patch patch in patches.Prefixes.Where((Patch p) => p.owner.Equals(this.ModManifest.UniqueID)))
            {
                sb.AppendLine().Append("\tPrefixed with method: ").Append(patch.PatchMethod.GetFullName());
            }
            foreach (Patch patch in patches.Postfixes.Where((Patch p) => p.owner.Equals(this.ModManifest.UniqueID)))
            {
                sb.AppendLine().Append("\tPostfixed with method: ").Append(patch.PatchMethod.GetFullName());
            }
            foreach (Patch patch in patches.Transpilers.Where((Patch p) => p.owner.Equals(this.ModManifest.UniqueID)))
            {
                sb.AppendLine().Append("\tTranspiled with method: ").Append(patch.PatchMethod.GetFullName());
            }
            foreach (Patch patch in patches.Finalizers.Where((Patch p) => p.owner.Equals(this.ModManifest.UniqueID)))
            {
                sb.AppendLine().Append("\tFinalized with method: ").Append(patch.PatchMethod.GetFullName());
            }
            ModMonitor.Log(sb.ToString(), LogLevel.Trace);
        }
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
        IModInfo gmcm = this.Helper.ModRegistry.Get("spacechase0.GenericModConfigMenu");
        if (gmcm is null)
        {
            this.Monitor.Log(I18n.GmcmNotFound(), LogLevel.Debug);
            return;
        }
        if (gmcm.Manifest.Version.IsOlderThan("1.8.0"))
        {
            this.Monitor.Log(I18n.GmcmVersionMessage(version: "1.8.0", currentversion: gmcm.Manifest.Version), LogLevel.Info);
            return;
        }

        var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null)
        {
            return;
        }

        configMenu.Register(
            mod: this.ModManifest,
            reset: () => Config = new ModConfig(),
            save: () => this.Helper.WriteConfig(Config)
            );

        configMenu.AddParagraph(
            mod: this.ModManifest,
            text: I18n.Mod_Description
            );

        configMenu.AddBoolOption(
            mod: this.ModManifest,
            getValue: () => Config.Enabled,
            setValue: value => Config.Enabled = value,
            name: I18n.Enabled_Title
            );

        configMenu.AddBoolOption(
            mod: this.ModManifest,
            getValue: () => Config.PreventRemovalFromTable,
            setValue: value => Config.PreventRemovalFromTable = value,
            name: I18n.TableRemoval_Title,
            tooltip: I18n.TableRemoval_Description);

        configMenu.AddKeybindList(
            mod: this.ModManifest,
            getValue: () => Config.FurniturePlacementKey,
            setValue: value => Config.FurniturePlacementKey = value,
            name: I18n.FurniturePlacementKey_Title,
            tooltip: I18n.FurniturePlacementKey_Description);
    }
}
