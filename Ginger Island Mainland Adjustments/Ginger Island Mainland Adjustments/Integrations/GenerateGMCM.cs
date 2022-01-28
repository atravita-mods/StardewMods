namespace GingerIslandMainlandAdjustments.Integrations;

/// <summary>
/// Class that generates the GMCM for this mod.
/// </summary>
internal static class GenerateGMCM
{
    /// <summary>
    /// Minimum version of GMCM to look for.
    /// </summary>
    /// <remarks>Integration will be disabled for lower version numbers.</remarks>
    private const string MINVERSION = "1.8.0";

    /// <summary>
    /// Generates the GMCM for this mod.
    /// </summary>
    /// <param name="manifest">The mod's manifest.</param>
    internal static void Build(IManifest manifest)
    {
        IModInfo gmcm = Globals.ModRegistry.Get("spacechase0.GenericModConfigMenu");
        if (gmcm is null)
        {
            Globals.ModMonitor.Log(I18n.GmcmNotFound(), LogLevel.Debug);
            return;
        }
        if (gmcm.Manifest.Version.IsOlderThan(MINVERSION))
        {
            Globals.ModMonitor.Log(I18n.GmcmVersionMessage(version: MINVERSION, currentversion: gmcm.Manifest.Version), LogLevel.Info);
            return;
        }
        IGenericModConfigMenuApi? configMenu = Globals.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null)
        {
            return;
        }

        configMenu.Register(
            mod: manifest,
            reset: () => Globals.Config = new ModConfig(),
            save: () => Globals.Helper.WriteConfig(Globals.Config));

        configMenu.AddParagraph(
            mod: manifest,
            text: I18n.ModDescription);

        configMenu.AddBoolOption(
            mod: manifest,
            name: I18n.Config_EnforceGITiming_Title,
            getValue: () => Globals.Config.EnforceGITiming,
            setValue: value => Globals.Config.EnforceGITiming = value,
            tooltip: I18n.Config_EnforceGITiming_Description);

        configMenu.AddBoolOption(
            mod: manifest,
            name: I18n.Config_Scheduler_Title,
            getValue: () => Globals.Config.UseThisScheduler,
            setValue: value => Globals.Config.UseThisScheduler = value,
            tooltip: I18n.Config_Scheduler_Description);

        configMenu.AddNumberOption(
            mod: manifest,
            name: I18n.Config_GroupChance_Title,
            getValue: () => Globals.Config.GroupChance,
            setValue: value => Globals.Config.GroupChance = value,
            tooltip: I18n.Config_GroupChance_Description,
            min: 0f,
            max: 1f);

        configMenu.AddNumberOption(
            mod: manifest,
            name: I18n.Config_ExplorerChance_Title,
            getValue: () => Globals.Config.ExplorerChance,
            setValue: value => Globals.Config.ExplorerChance = value,
            tooltip: I18n.Config_ExplorerChance_Description,
            min: 0f,
            max: 1f);

        configMenu.AddTextOption(
            mod: manifest,
            name: I18n.Config_GusDay_Title,
            getValue: () => Globals.Config.GusDay.ToString(),
            setValue: value => Globals.Config.GusDay = ModConfig.TryParseDayOfWeekOrGetDefault(value),
            tooltip: I18n.Config_GusDay_Description,
            allowedValues: Enum.GetNames(typeof(DayOfWeek)),
            formatAllowedValue: value => I18n.GetByKey(value));

        configMenu.AddNumberOption(
            mod: manifest,
            name: I18n.Config_GusChance_Title,
            getValue: () => Globals.Config.GusChance,
            setValue: value => Globals.Config.GusChance = value,
            tooltip: I18n.Config_GusChance_Description,
            min: 0f,
            max: 1f);
    }
}