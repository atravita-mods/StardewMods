﻿using System.Reflection;

using AtraCore.Framework.Caches;

using AtraShared.Integrations;
using AtraShared.Utils.Extensions;
using GingerIslandMainlandAdjustments.Configuration;

namespace GingerIslandMainlandAdjustments.Integrations;

/// <summary>
/// Class that generates the GMCM for this mod.
/// </summary>
internal static class GenerateGMCM
{
    private static GMCMHelper? helper;

    /// <summary>
    /// Grabs the GMCM api for this mod.
    /// </summary>
    /// <param name="manifest">The mod's manifest.</param>
    /// <param name="translation">The translation helper.</param>
    internal static void Initialize(IManifest manifest, ITranslationHelper translation)
    {
        helper = new(Globals.ModMonitor, translation, Globals.ModRegistry, manifest);
        helper.TryGetAPI();
    }

    /// <summary>
    /// Generates the GMCM for this mod.
    /// </summary>
    internal static void Build()
    {
        if (!(helper?.HasGottenAPI == true))
        {
            return;
        }

        helper.Unregister();
        helper.Register(
                reset: static () => Globals.Config = new ModConfig(),
                save: static () => Globals.Helper.AsyncWriteConfig(Globals.ModMonitor, Globals.Config))
            .AddParagraph(I18n.ModDescription)
            .AddBoolOption(
                name: I18n.Config_EnforceGITiming_Title,
                getValue: static () => Globals.Config.EnforceGITiming,
                setValue: static value => Globals.Config.EnforceGITiming = value,
                tooltip: I18n.Config_EnforceGITiming_Description)
            .AddBoolOption(
                name: I18n.Config_RequireResortDialogue_Title,
                getValue: static () => Globals.Config.RequireResortDialogue,
                setValue: static value => Globals.Config.RequireResortDialogue = value,
                tooltip: I18n.Config_RequireResortDialogue_Description)
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
                max: 15)
            .AddBoolOption(
                name: I18n.Config_Stage_Title,
                getValue: static () => Globals.Config.StageFarNpcsAtSaloon,
                setValue: static value => Globals.Config.StageFarNpcsAtSaloon = value,
                tooltip: I18n.Config_Stage_Description)
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
            if (property.Name.StartsWith("Allow", StringComparison.OrdinalIgnoreCase))
            {
                if (property.PropertyType == typeof(bool))
                {
                    helper.AddBoolOption(property, () => Globals.Config);
                }
                else if (property.PropertyType == typeof(VillagerExclusionOverride))
                {
                    helper.AddEnumOption<ModConfig, VillagerExclusionOverride>(property, () => Globals.Config);
                }
            }
        }
    }

    internal static void BuildNPCDictionary()
    {
        if (!(helper?.HasGottenAPI == true))
        {
            return;
        }

        Globals.Config.PopulateScheduleStrictness();

        helper.AddPageHere("strictness", I18n.ScheduleStrictness, I18n.ScheduleStrictness_Description)
              .AddParagraph(I18n.ScheduleStrictness_Description);
        foreach ((string k, ScheduleStrictness v) in Globals.Config.ScheduleStrictness)
        {
            helper.AddEnumOption(
                name: () => NPCCache.GetByVillagerName(k)?.displayName ?? k,
                getValue: () => Globals.Config.ScheduleStrictness.TryGetValue(k, out ScheduleStrictness val) ? val : ScheduleStrictness.Default,
                setValue: (value) => Globals.Config.ScheduleStrictness[k] = value);
        }

        Globals.Helper.AsyncWriteConfig(Globals.ModMonitor, Globals.Config);
    }

    private static string TwoPlaceFixedPoint(float f) => $"{f:f2}";
}