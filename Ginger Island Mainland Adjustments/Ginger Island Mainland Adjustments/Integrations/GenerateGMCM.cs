using System.Reflection;
using AtraShared.Integrations;
using GingerIslandMainlandAdjustments.Configuration;

namespace GingerIslandMainlandAdjustments.Integrations;

/// <summary>
/// Class that generates the GMCM for this mod.
/// </summary>
internal static class GenerateGMCM
{
    /// <summary>
    /// Generates the GMCM for this mod.
    /// </summary>
    /// <param name="manifest">The mod's manifest.</param>
    /// <param name="translation">The translation helper.</param>
    internal static void Build(IManifest manifest, ITranslationHelper translation)
    {
        GMCMHelper helper = new(Globals.ModMonitor, translation, Globals.ModRegistry, manifest);
        if (!helper.TryGetAPI())
        {
            return;
        }

        helper.Register(
                reset: static () => Globals.Config = new ModConfig(),
                save: static () => Globals.Helper.WriteConfig(Globals.Config))
            .AddParagraph(I18n.ModDescription)
            .AddBoolOption(
                name: I18n.Config_EnforceGITiming_Title,
                getValue: static () => Globals.Config.EnforceGITiming,
                setValue: static value => Globals.Config.EnforceGITiming = value,
                tooltip: I18n.Config_EnforceGITiming_Description)
            .AddEnumOption(
                name: I18n.Config_WearIslandClothing_Title,
                getValue: static () => Globals.Config.WearIslandClothing,
                setValue: static value => Globals.Config.WearIslandClothing = value,
                tooltip: I18n.Config_WearIslandClothing_Description)
            .AddBoolOption(
                name: I18n.Config_Scheduler_Title,
                getValue: static () => Globals.Config.UseThisScheduler,
                setValue: static value => Globals.Config.UseThisScheduler = value,
                tooltip: I18n.Config_Scheduler_Description)
            .AddParagraph(I18n.Config_Scheduler_Otheroptions)
            .AddNumberOption(
                name: I18n.Config_Capacity_Title,
                getValue: static () => Globals.Config.Capacity,
                setValue: static value => Globals.Config.Capacity = value,
                tooltip: I18n.Config_Capacity_Description,
                min: 0,
                max: 12)
            .AddNumberOption(
                name: I18n.Config_GroupChance_Title,
                getValue: static () => Globals.Config.GroupChance,
                setValue: static value => Globals.Config.GroupChance = value,
                tooltip: I18n.Config_GroupChance_Description,
                formatValue: TwoPlaceFixedPoint,
                min: 0f,
                max: 1f,
                interval: 0.01f)
            .AddNumberOption(
                name: I18n.Config_ExplorerChance_Title,
                getValue: static () => Globals.Config.ExplorerChance,
                setValue: static value => Globals.Config.ExplorerChance = value,
                tooltip: I18n.Config_ExplorerChance_Description,
                formatValue: TwoPlaceFixedPoint,
                min: 0f,
                max: 1f,
                interval: 0.01f)
            .AddEnumOption(
                name: I18n.Config_GusDay_Title,
                getValue: static () => Globals.Config.GusDay,
                setValue: static value => Globals.Config.GusDay = value,
                tooltip: I18n.Config_GusDay_Description)
            .AddNumberOption(
                name: I18n.Config_GusChance_Title,
                getValue: static () => Globals.Config.GusChance,
                setValue: static value => Globals.Config.GusChance = value,
                tooltip: I18n.Config_GusChance_Description,
                formatValue: TwoPlaceFixedPoint,
                min: 0f,
                max: 1f,
                interval: 0.01f);

        foreach (PropertyInfo property in typeof(ModConfig).GetProperties())
        {
            if (property.Name.StartsWith("Allow"))
            {
                helper.AddBoolOption(property, () => Globals.Config);
            }
        }
    }

    private static string TwoPlaceFixedPoint(float f) => $"{f:f2}";
}