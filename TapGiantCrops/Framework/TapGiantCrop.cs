using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;

namespace TapGiantCrops.Framework;

/// <summary>
/// API instance for Tap Giant Crops.
/// </summary>
public class TapGiantCrop : ITapGiantCropsAPI
{
    /// <inheritdoc />
    public bool CanPlaceTapper(GameLocation loc, Vector2 tile) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool TryPlaceTapper(GameLocation loc, Vector2 tile) => throw new NotImplementedException();

    private GiantCrop? GetGiantCropAt(GameLocation loc, Vector2 tile)
    {
        Vector2 offset = tile;
        offset.X -= 1;
        offset.Y -= 2;
        foreach(ResourceClump? clump in loc.resourceClumps)
        {
            if (clump is GiantCrop crop && crop.tile.Value == offset)
            {
                return crop;
            }
        }
        return null;
    }
}
