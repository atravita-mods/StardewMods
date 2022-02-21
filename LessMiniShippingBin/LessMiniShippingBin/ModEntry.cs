using System.Reflection;
using System.Text;
using HarmonyLib;
using StardewModdingAPI.Events;

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
        try
        {
            Config = this.Helper.ReadConfig<ModConfig>();
        }
        catch
        {
            ModMonitor.Log(I18n.IllFormatedConfig(), LogLevel.Warn);
            Config = new();
        }
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
    /// Generates the GMCM for this mod by looking at the structure of the config class.
    /// </summary>
    /// <param name="sender">Unknown, expected by SMAPI.</param>
    /// <param name="e">Arguments for eevnt.</param>
    /// <remarks>To add a new setting, add the details to the i18n file. Currently handles: bool.</remarks>
    private void SetUpConfig(object? sender, GameLaunchedEventArgs e)
    {
        IModInfo gmcm = this.Helper.ModRegistry.Get("spacechase0.GenericModConfigMenu");
        if (gmcm is null)
        {
            this.Monitor.Log(I18n.GmcmNotFound(), LogLevel.Debug);
            return;
        }
        if (gmcm.Manifest.Version.IsOlderThan("1.6.0"))
        {
            this.Monitor.Log(I18n.GmcmVersionMessage(version: "1.6.0", currentversion: gmcm.Manifest.Version), LogLevel.Info);
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
            save: () => this.Helper.WriteConfig(Config));

        configMenu.AddParagraph(
            mod: this.ModManifest,
            text: I18n.Mod_Description);

        configMenu.AddNumberOption(
            mod: this.ModManifest,
            getValue: () => Config.MiniShippingCapacity,
            setValue: value => Config.MiniShippingCapacity = value,
            name: I18n.Config_Capacity_Title,
            tooltip: I18n.Config_Capacity_Description,
            min: 9,
            max: 48,
            interval: 9);

        configMenu.AddNumberOption(
            mod: this.ModManifest,
            getValue: () => Config.JuminoCapcaity,
            setValue: value => Config.JuminoCapcaity = value,
            name: I18n.Config_Junimo_Title,
            tooltip: I18n.Config_Junimo_Description,
            min: 9,
            max: 48,
            interval: 9);
    }

    /// <summary>
    /// Log to DEBUG if compiled with DEBUG
    /// Log to verbose only otherwise.
    /// </summary>
    /// <param name="message">Message to log.</param>
    private void DebugLog(string message, LogLevel level = LogLevel.Debug)
    {
#if DEBUG
        this.Monitor.Log(message, level);
#else
        this.Monitor.VerboseLog(message);
#endif
    }
}
