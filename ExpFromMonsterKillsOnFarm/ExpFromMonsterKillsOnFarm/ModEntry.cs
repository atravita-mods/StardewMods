using System.Reflection;
using System.Text;
using HarmonyLib;
using StardewModdingAPI.Events;

namespace ExpFromMonsterKillsOnFarm;

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

        foreach (PropertyInfo property in typeof(ModConfig).GetProperties())
        {
            MethodInfo? getter = property.GetGetMethod();
            MethodInfo? setter = property.GetSetMethod();
            if (getter is null || setter is null)
            {
                this.DebugLog("Config appears to have a mis-formed option?");
                continue;
            }

            if (property.PropertyType.Equals(typeof(bool)))
            {
                var getterDelegate = (Func<ModConfig, bool>)Delegate.CreateDelegate(typeof(Func<ModConfig, bool>), getter);
                var setterDelegate = (Action<ModConfig, bool>)Delegate.CreateDelegate(typeof(Action<ModConfig, bool>), setter);

                configMenu.AddBoolOption(
                    mod: this.ModManifest,
                    getValue: () => getterDelegate(Config),
                    setValue: (bool value) => setterDelegate(Config, value),
                    name: () => I18n.GetByKey($"config.{property.Name}.title"),
                    tooltip: () => I18n.GetByKey($"config.{property.Name}.description"));
            }
            else
            {
                this.DebugLog($"{property.Name} unaccounted for.", LogLevel.Warn);
            }
        }
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
        Monitor.VerboseLog(message);
#endif
    }
}
