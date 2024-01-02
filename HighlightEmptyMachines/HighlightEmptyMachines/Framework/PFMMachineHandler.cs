using System.Runtime.CompilerServices;

using AtraBase.Collections;
using AtraBase.Toolkit;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.Utils.Extensions;
using StardewModdingAPI.Utilities;

namespace HighlightEmptyMachines.Framework;

/// <summary>
/// Static class that handles PFM machines.
/// </summary>
internal static class PFMMachineHandler
{
    private static readonly PerScreen<Dictionary<string, MachineStatus>> ValidMachinesPerScreen = new(() => new Dictionary<string, MachineStatus>());
    private static readonly DefaultDict<string, HashSet<PFMMachineData>> Recipes = new(() => new HashSet<PFMMachineData>());
    private static readonly HashSet<string> HasUnconditionalRecipe = [];
    private static IProducerFrameworkModAPI? pfmAPI = null;

    /// <summary>
    /// Gets a lookup table between machines and their current status.
    /// </summary>
    internal static Dictionary<string, MachineStatus> ValidMachines => ValidMachinesPerScreen.Value;

    /// <summary>
    /// Gets a list of conditional PFM machines.
    /// </summary>
    internal static ICollection<string> ConditionalPFMMachines => Recipes.Keys;

    /// <summary>
    /// Gets a list of unconditional PFM machines.
    /// </summary>
    internal static ICollection<string> UnconditionalPFMMachines => HasUnconditionalRecipe;

    private static List<Dictionary<string, object>>? MachineRecipes => pfmAPI?.GetRecipes();

    /// <summary>
    /// Tries to grab the PFM api.
    /// </summary>
    /// <param name="modRegistry">ModRegistry.</param>
    /// <returns>True if API grabbed, false otherwise.</returns>
    internal static bool TryGetAPI(IModRegistry modRegistry)
        => new IntegrationHelper(ModEntry.ModMonitor, ModEntry.TranslationHelper, modRegistry)
            .TryGetAPI("Digus.ProducerFrameworkMod", "1.7.4", out pfmAPI);

    /// <summary>
    /// Prints the pfm machine recipes to console.
    /// </summary>
    /// <param name="command">Command name.</param>
    /// <param name="args">Command arguments.</param>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Expected format of SMAPI commands")]
    internal static void PrintPFMRecipes(string command, string[] args)
    {
        ModEntry.ModMonitor.Log("PFM Machine Recipes", LogLevel.Info);
        foreach ((string id, HashSet<PFMMachineData> v) in Recipes)
        {
            ModEntry.ModMonitor.Log($"Looking at machine {id.GetBigCraftableName()}", LogLevel.Info);
            foreach (PFMMachineData recipe in v)
            {
                ModEntry.ModMonitor.Log('\t' + recipe.ToString(), LogLevel.Info);
            }
        }
    }

    /// <summary>
    /// Run at save load, looks up the PFM machines and saves their data.
    /// </summary>
    internal static void ProcessPFMRecipes()
    {
        List<Dictionary<string, object>>? recipes = MachineRecipes;

        if (recipes is null || recipes.Count == 0)
        {
            ModEntry.ModMonitor.Log($"PFM recipes not found?", LogLevel.Warn);
            return;
        }

        Recipes.Clear();
        HasUnconditionalRecipe.Clear();

        foreach (Dictionary<string, object>? item in recipes)
        {
            if (!item.TryGetValue("MachineID", out object? id) || id is not string itemID || HasUnconditionalRecipe.Contains(itemID))
            {
                continue;
            }

            bool outdoorsOnly = item.TryGetValue("RequiredOutdoors", out object? val) && val is bool b && b;

            StardewSeasons seasons;
            if (item.TryGetValue("RequiredSeason", out object? seasonList)
                && seasonList is List<string> seasonListActual && seasonListActual.Count > 0)
            {
                seasons = seasonListActual.ParseSeasonList();
            }
            else
            {
                seasons = StardewSeasons.All;
            }

            StardewWeather weather;
            if (item.TryGetValue("RequiredWeather", out object? weatherList)
                && weatherList is List<string> weatherListActual && weatherListActual.Count > 0)
            {
                weather = weatherListActual.ParseWeatherList();
            }
            else
            {
                weather = StardewWeather.All;
            }

            string[]? validLocations = item.TryGetValue("RequiredLocation", out object? locs) && locs is List<string> locationList && locationList.Count > 0
                    ? locationList.ToArray()
                    : null;

            if (!outdoorsOnly && weather == StardewWeather.All && seasons == StardewSeasons.All && validLocations is null)
            {
                ModEntry.ModMonitor.DebugOnlyLog($"{itemID} is unconditional.", LogLevel.Trace);
                HasUnconditionalRecipe.Add(itemID);
                Recipes.Remove(itemID);

                continue;
            }

            PFMMachineData recipe = new(
                OutdoorsOnly: outdoorsOnly,
                ValidLocations: validLocations,
                AllowedSeasons: seasons,
                AllowedWeathers: weather);
            Recipes[itemID].Add(recipe);
        }

        ModEntry.ModMonitor.DebugOnlyLog($"{recipes.Count} recipes indexed - {Recipes.Count} conditional machines and - {HasUnconditionalRecipe.Count} unconditional machines.");
    }

    /// <summary>
    /// Refreshes the validity list.
    /// </summary>
    /// <param name="location">The location to analyze.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal static void RefreshValidityList(GameLocation? location)
    {
        if (location is null || pfmAPI is null)
        {
            return;
        }

        ValidMachines.Clear();

        // unconditional machines
        foreach (string machine in HasUnconditionalRecipe)
        {
            if (ModEntry.Config.ProducerFrameworkModMachines.TryGetValue(machine.GetBigCraftableName(), out bool setting) && setting)
            {
                ModEntry.ModMonitor.DebugOnlyLog($"{machine.GetBigCraftableName()} is enabled unconditionally.");
                ValidMachines[machine] = MachineStatus.Enabled;
            }
            else
            {
                ModEntry.ModMonitor.DebugOnlyLog($"{machine.GetBigCraftableName()} is disabled in config.");
                ValidMachines[machine] = MachineStatus.Disabled;
            }
        }

        if (ConditionalPFMMachines.Count == 0)
        {
            return;
        }

        StardewSeasons season = SeasonExtensions.GetSeasonFromGame(location);
        StardewWeather weather = GetPFMWeather();
        bool isOutDoors = location.IsOutdoors;

        // conditional machines.
        foreach (string machine in ConditionalPFMMachines)
        {
            if (ModEntry.Config.ProducerFrameworkModMachines.TryGetValue(machine.GetBigCraftableName(), out bool setting) && setting)
            {
                foreach (PFMMachineData recipe in Recipes[machine])
                {
                    if ((isOutDoors || !recipe.OutdoorsOnly)
                        && recipe.AllowedSeasons.HasFlag(season)
                        && recipe.AllowedWeathers.HasFlag(weather)
                        && (recipe.ValidLocations is null || recipe.ValidLocations.Contains(location.Name)))
                    {
                        ValidMachines[machine] = MachineStatus.Enabled;
                        ModEntry.ModMonitor.DebugOnlyLog($"{machine.GetBigCraftableName()} is enabled.");
                        goto Continue;
                    }
                }
                ModEntry.ModMonitor.DebugOnlyLog($"{machine.GetBigCraftableName()} is invalid");
                ValidMachines[machine] = MachineStatus.Invalid;
            }
            else
            {
                ModEntry.ModMonitor.DebugOnlyLog($"{machine.GetBigCraftableName()} is disabled in config.");
                ValidMachines[machine] = MachineStatus.Disabled;
            }
Continue:
            ;
        }
    }

    /// <summary>
    /// Get's PFM's idea of the current weather.
    /// </summary>
    /// <returns>Weather enum.</returns>
    private static StardewWeather GetPFMWeather()
    {
        if (Utility.isFestivalDay(Game1.dayOfMonth, Game1.season) || (SaveGame.loaded?.weddingToday ?? Game1.weddingToday))
        {
            return StardewWeather.Sunny;
        }
        else if (Game1.isSnowing)
        {
            return StardewWeather.Snowy;
        }
        else if (Game1.isRaining)
        {
            return Game1.isLightning ? StardewWeather.Stormy : StardewWeather.Rainy;
        }
        else if (SaveGame.loaded?.isDebrisWeather ?? Game1.isDebrisWeather)
        {
            return StardewWeather.Windy;
        }
        return StardewWeather.Sunny;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PFMMachineData"/> struct.
    /// </summary>
    /// <param name="OutdoorsOnly">Whether the machine recipe is outdoors only.</param>
    /// <param name="ValidLocations">Whether there's location limitations.</param>
    /// <param name="AllowedSeasons">Whether there's season limitations.</param>
    /// <param name="AllowedWeathers">Whether there's weather limitations.</param>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopErrorConsts.IsRecord)]
    internal readonly record struct PFMMachineData(bool OutdoorsOnly, string[]? ValidLocations, StardewSeasons AllowedSeasons, StardewWeather AllowedWeathers);
}