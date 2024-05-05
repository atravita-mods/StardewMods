using System.Reflection.Emit;
using AtraBase.Toolkit;
using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;

using Microsoft.Xna.Framework.Content;

namespace HolidaySales;

/// <summary>
/// Utilities for this mod.
/// </summary>
internal static class HSUtils
{
    private static readonly ThreadLocal<HashSet<string>> _visited = new(static () => new());

    /// <summary>
    /// looks for calls to Utility.isFestivalDay and replaces it with calls to my custom method here.
    /// </summary>
    /// <param name="helper">ILHelper.</param>
    internal static void AdjustIsFestivalCall(this ILHelper helper)
    {
        helper.ForEachMatch(
            [
                new(OpCodes.Call, typeof(Utility).GetCachedMethod(nameof(Utility.isFestivalDay), ReflectionCache.FlagTypes.StaticFlags, Type.EmptyTypes)),
            ],
            (helper) =>
            {
                helper.ReplaceOperand(typeof(HSUtils).GetCachedMethod(nameof(IsFestivalDayAdjustedForConfig), ReflectionCache.FlagTypes.StaticFlags));
                helper.Insert(
                [
                    new(OpCodes.Call, typeof(Game1).GetCachedProperty(nameof(Game1.currentLocation), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod()),
                    new(OpCodes.Callvirt, typeof(GameLocation).GetCachedProperty(nameof(GameLocation.Name), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
                ]);
                return true;
            });
    }

    /// <summary>
    /// Adjusts a call to Utility.isFestival to check for the town only.
    /// </summary>
    /// <param name="helper">ILHelper.</param>
    internal static void AdjustIsFestivalCallForTown(this ILHelper helper)
    {
        helper.ForEachMatch(
            [
                new(OpCodes.Call, typeof(Utility).GetCachedMethod(nameof(Utility.isFestivalDay), ReflectionCache.FlagTypes.StaticFlags, Type.EmptyTypes)),
            ],
            (helper) =>
            {
                helper.ReplaceOperand(typeof(HSUtils).GetCachedMethod(nameof(IsFestivalDayAdjustedForConfig), ReflectionCache.FlagTypes.StaticFlags))
                      .GetLabels(out IList<Label>? labelsToMove)
                      .Insert(
                [
                    new CodeInstruction(OpCodes.Ldstr, "Town").WithLabels(labelsToMove),
                ]);
                return true;
        });
    }

    /// <summary>
    /// Whether or not stores are closed for the festival after adjustments.
    /// </summary>
    /// <returns>true if the store is closed, false otherwise.</returns>
    internal static bool StoresClosedForFestival()
    {
        if (IsFestivalDayAdjustedForConfig("Town"))
        {
            return Utility.getStartTimeOfFestival() < 1900;
        }
        return false;
    }

    /// <summary>
    /// Whether or not the festival should be open or something.
    /// </summary>
    /// <param name="mapname">Map to search for.</param>
    /// <returns>If it should be considered a festival day for this specific config.</returns>
    internal static bool IsFestivalDayAdjustedForConfig(string mapname)
    {
        return ModEntry.Config.StoreFestivalBehavior switch
        {
            FestivalsShopBehavior.Open => false,
            FestivalsShopBehavior.Closed => Utility.isFestivalDay(),
            FestivalsShopBehavior.MapDependent => IsFestivalDayForMap(Game1.dayOfMonth, Game1.season, mapname),
            _ => TKThrowHelper.ThrowUnexpectedEnumValueException<FestivalsShopBehavior, bool>(ModEntry.Config.StoreFestivalBehavior),
        };
    }

    /// <summary>
    /// Whether or not it should be considered a festival date for that particular map.
    /// </summary>
    /// <param name="day">day.</param>
    /// <param name="season">season.</param>
    /// <param name="mapname">the map name.</param>
    /// <returns>true if it should be considered a festival day.</returns>
    internal static bool IsFestivalDayForMap(int day, Season season, string mapname)
    {
        string? s = Utility.getSeasonKey(season) + day;
        if (Game1.temporaryContent.Load<Dictionary<string, string>>(@"Data\Festivals\FestivalDates").ContainsKey(s))
        {
            ReadOnlySpan<char> mapRegion = [];

            if (Game1.getLocationFromName(mapname) is { } loc)
            {
                var contextId = loc.GetLocationContextId();

                if (Game1.locationContextData.TryGetValue(contextId, out var context))
                {
                    var visited = _visited.Value!;
                    visited.Clear();
                    visited.Add(contextId);

                    while (context.CopyWeatherFromLocation is { } next)
                    {
                        if (!Game1.locationContextData.TryGetValue(next, out var nextData))
                        {
                            ModEntry.ModMonitor.Log($"Could not find location data corresponding to {next}, skipping.");
                            break;
                        }
                        else
                        {
                            contextId = next;
                            context = nextData;
                        }
                    }

                    if (contextId != "Default")
                    {
                        mapRegion = contextId;
                    }
                }
            }

            if (mapRegion.IsEmpty)
            {
                ReadOnlySpan<char> mapNameSpan = mapname.AsSpan();
                string default_area = "Town";

                if (mapNameSpan.StartsWith("Custom_", StringComparison.Ordinal))
                {
                    mapNameSpan = mapNameSpan["Custom_".Length..];
                    default_area = "CustomArea";
                }
                int index = mapNameSpan.IndexOf('_');
                mapRegion = index == -1 ? default_area : mapNameSpan[..index];
            }

            if (mapRegion.IsEmpty)
            {
                mapRegion = "Town";
            }

            try
            {
                Dictionary<string, string>? festivaldata = Game1.temporaryContent.Load<Dictionary<string, string>>($@"Data\Festivals\{s}");
                if (festivaldata.TryGetValue("conditions", out string? conditionsStr))
                {
                    ReadOnlySpan<char> conditions = conditionsStr.GetNthChunk('/', 0).Trim();

                    ModEntry.ModMonitor.DebugOnlyLog($"Testing {conditions.ToString()} against {mapRegion.ToString()}");
                    if (conditions.Equals(mapRegion, StringComparison.Ordinal))
                    {
                        return true;
                    }
                    else if (!conditions.StartsWith("Custom", StringComparison.Ordinal) && mapRegion == "Town")
                    {
                        return true;
                    }
                    else if (conditions.StartsWith("Custom", StringComparison.Ordinal) && mapRegion == "CustomMapRegion")
                    {
                        return conditions.IndexOf('_') == conditions.LastIndexOf('_');
                    }
                    return false;
                }
            }
            catch (ContentLoadException)
            {
                ModEntry.ModMonitor.Log($"Festival data for {season} {day} was not found.", LogLevel.Warn);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError($"loading festival data for {season} {day}", ex);
            }
        }
        return false;
    }
}
