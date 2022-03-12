using StardewValley.Buildings;
using StardewValley.Locations;

namespace AtraShared.Utils;

// TODO: Remove checks for BuildableGameLocation in 1.6.

/// <summary>
/// Utility for gamelocations.
/// </summary>
public static class GameLocationUtils
{
    /// <summary>
    /// Yields all game locations.
    /// </summary>
    /// <returns>IEnumerable of all game locations.</returns>
    public static IEnumerable<GameLocation> YieldAllLocations()
    {
        foreach (GameLocation location in Game1.locations)
        {
            yield return location;
            if (location is BuildableGameLocation buildableloc)
            {
                foreach (GameLocation loc in YieldInteriorLocations(buildableloc))
                {
                    yield return loc;
                }
            }
        }
    }

    private static IEnumerable<GameLocation> YieldInteriorLocations(BuildableGameLocation loc)
    {
        foreach (Building building in loc.buildings)
        {
            if (building.indoors?.Value is GameLocation indoorloc)
            {
                yield return indoorloc;
                if (indoorloc is BuildableGameLocation buildableIndoorLoc)
                {
                    foreach (GameLocation nestedLocation in YieldInteriorLocations(buildableIndoorLoc))
                    {
                        yield return nestedLocation;
                    }
                }
            }
        }
    }
}