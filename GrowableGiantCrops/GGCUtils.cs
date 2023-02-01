using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using Microsoft.Xna.Framework;

using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops;

/// <summary>
/// The utility class for this mod.
/// </summary>
internal static class GGCUtils
{
    /// <summary>
    /// Checks to see if I can stick a resource clump at a specific tile.
    /// </summary>
    /// <param name="location">Game location to check.</param>
    /// <param name="tileX">x coordinate of tile.</param>
    /// <param name="tileY">y coordinate of tile.</param>
    /// <param name="relaxed">whether or not to use relaxed placement rules.</param>
    /// <returns>True if placeable, false otherwise.</returns>
    [MethodImpl(TKConstants.Hot)]
    internal static bool IsTilePlaceableForResourceClump(GameLocation location, int tileX, int tileY, bool relaxed)
    {
        if (location is null || location.doesTileHaveProperty(tileX, tileY, "Water", "Back") is not null)
        {
            return false;
        }

        Rectangle position = new(tileX * 64, tileY * 64, 64, 64);

        foreach (Farmer farmer in location.farmers)
        {
            if (farmer.GetBoundingBox().Intersects(position))
            {
                return false;
            }
        }

        foreach (Character character in location.characters)
        {
            if (character.GetBoundingBox().Intersects(position))
            {
                return false;
            }
        }

        foreach (ResourceClump? clump in location.resourceClumps)
        {
            if (clump.getBoundingBox(clump.currentTileLocation).Intersects(position))
            {
                return false;
            }
        }

        if (location is IAnimalLocation hasAnimals)
        {
            foreach (FarmAnimal? animal in hasAnimals.Animals.Values)
            {
                if (animal.GetBoundingBox().Intersects(position))
                {
                    return false;
                }
            }
        }

        if (!relaxed && location is BuildableGameLocation buildable)
        {
            foreach (Building? building in buildable.buildings)
            {
                if (!building.isTilePassable(new Vector2(tileX, tileY)))
                {
                    return false;
                }
            }
        }

        return relaxed || !location.isTileOccupied(new Vector2(tileX, tileY));
    }
}
