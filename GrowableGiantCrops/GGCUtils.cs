using System.Runtime.CompilerServices;

using AtraBase.Toolkit;
using AtraBase.Toolkit.Extensions;

using AtraShared.Wrappers;

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
    /// Checks to see if this tile is valid for trees.
    /// </summary>
    /// <param name="location">The game location to check.</param>
    /// <param name="relaxed">Whether or not to use relaxed placement restrictions.</param>
    /// <param name="tileX">the X tile location.</param>
    /// <param name="tileY">the Y tile location.</param>
    /// <returns>True if it's placeable, false otherwise.</returns>
    internal static bool CanPlantTreesAtLocation(GameLocation? location, bool relaxed, int tileX, int tileY)
    {
        if (location?.terrainFeatures is null || Utility.isPlacementForbiddenHere(location))
        {
            return false;
        }
        if (relaxed || (location.IsOutdoors && location.doesTileHavePropertyNoNull(tileX, tileY, "Type", "Back") == "Dirt"))
        {
            return true;
        }
        return location.IsGreenhouse || location.map?.Properties?.ContainsKey("ForceAllowTreePlanting") == true;
    }

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

    /// <summary>
    /// Tries to get the name of an SObject.
    /// </summary>
    /// <param name="idx">index of that SOBject.</param>
    /// <returns>Name or placeholder if not found.</returns>
    internal static string GetNameOfSObject(int idx)
        => Game1Wrappers.ObjectInfo.TryGetValue(idx, out string? data)
            ? data.GetNthChunk('/').ToString()
            : "NoNameFound";

    internal static bool HasTreeInRadiusTwo(this GameLocation? loc, int tileX, int tileY)
    {
        if (loc?.terrainFeatures is null)
        {
            return false;
        }
        for (int x = tileX - 2; x <= tileX + 2; x++)
        {
            for (int y = tileY - 2; y <= tileY + 2; y++)
            {
                Vector2 tile = new(x, y);
                if (loc.terrainFeatures.TryGetValue(tile, out var terrain)
                    && terrain is Tree or FruitTree)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
