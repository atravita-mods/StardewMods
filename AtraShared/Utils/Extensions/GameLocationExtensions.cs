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
        => !location.IsFarm && !location.IsGreenhouse && location is not SlimeHutch && location is not Town && location is not IslandWest
            && (location is MineShaft or VolcanoDungeon || location.characters.Any((character) => character is Monster));
}