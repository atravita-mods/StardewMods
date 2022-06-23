using AtraBase.Toolkit.StringHandler;
using StardewValley.Locations;
using StardewValley.Monsters;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extensions on GameLocation.
/// </summary>
internal static class GameLocationExtensions
{
    /// <summary>
    /// Should this location be considered dangerous?
    /// Always safe: Farm, town, IslandWest.
    /// Always dangerous: Volcano, MineShaft.
    /// In-between: everywhere else.
    /// </summary>
    /// <param name="location">Location to check.</param>
    /// <returns>Whether the location should be considered dangerous.</returns>
    internal static bool IsDangerousLocation(this GameLocation location)
        => !location.IsFarm && !location.IsGreenhouse && location is not (SlimeHutch or Town or IslandWest)
            && (location is MineShaft or VolcanoDungeon or BugLand || location.characters.Any((character) => character is Monster));

    /// <summary>
    /// Returns true if there's a festival at a location and the player can't actually warp there yet.
    /// </summary>
    /// <param name="location">Location to check.</param>
    /// <param name="monitor">Logger.</param>
    /// <param name="alertPlayer">Whether or not to show a notification.</param>
    /// <returns>True if there's a festival at this location and it's before the start time, false otherwise.</returns>
    internal static bool IsBeforeFestivalAtLocation(this GameLocation location, IMonitor monitor, bool alertPlayer = false)
    {
        try
        {
            if (Game1.weatherIcon == 1)
            {
                Dictionary<string, string>? festivalData;
                try
                {
                    festivalData = Game1.temporaryContent.Load<Dictionary<string, string>>($@"Data\Festivals\{Game1.currentSeason}{Game1.dayOfMonth}");
                }
                catch (Exception ex)
                {
                    monitor.Log($"No festival file found for today....did someone screw with the time?\n\n{ex}", LogLevel.Warn);
                    return false;
                }
                if (festivalData.TryGetValue("conditions", out string? val))
                {
                    SpanSplit splits = val.SpanSplit('/');
                    if (splits.CountIsAtLeast(2))
                    {
                        if (!location.Name.Equals(splits[0].Word.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                        if (int.TryParse(splits[1].SpanSplit()[0], out int startTime) && Game1.timeOfDay < startTime)
                        {
                            if (alertPlayer)
                            {
                                Game1.drawObjectDialogue(Game1.content.LoadString(@"Strings\StringsFromCSFiles:Game1.cs.2973"));
                            }
                            return true;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            monitor.Log($"Mod failed while trying to find festival days....\n\n{ex}", LogLevel.Error);
        }
        return false;
    }
}