using GrowableGiantCrops.Framework.InventoryModels;

using Microsoft.Xna.Framework;

using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// The API for this mod.
/// </summary>
public sealed class Api : IGrowableGiantCropsAPI
{
    #region shovel

    /// <inheritdoc />
    public bool IsShovel(Tool tool) => tool is ShovelTool;

    /// <inheritdoc />
    public Tool GetShovel() => new ShovelTool();

    #endregion

    #region any

    /// <inheritdoc/>
    public bool CanPlace(SObject obj, GameLocation loc, Vector2 tile, bool relaxed)
    => obj switch
        {
            InventoryResourceClump clump => this.CanPlaceClump(clump, loc, tile, relaxed),
            _ => false,
        };

    #endregion

    #region clumps

    /// <inheritdoc />
    public ResourceClumpIndexes GetIndexOfClumpIfApplicable(SObject obj)
    {
        if (obj is InventoryResourceClump clump)
        {
            ResourceClumpIndexes idx = (ResourceClumpIndexes)clump.ParentSheetIndex;
            if (ResourceClumpIndexesExtensions.IsDefined(idx))
            {
                return idx;
            }
        }
        return ResourceClumpIndexes.Invalid;
    }

    /// <inheritdoc />
    public SObject GetResourceClump(ResourceClumpIndexes idx) => new InventoryResourceClump(idx, 1);

    /// <inheritdoc />
    public bool CanPlaceClump(SObject obj, GameLocation loc, Vector2 tile, bool relaxed)
        => obj is InventoryResourceClump clump && clump.CanPlace(loc, tile, relaxed);

    /// <inheritdoc />
    public bool TryPlaceClump(SObject obj, GameLocation loc, Vector2 tile, bool relaxed)
        => obj is InventoryResourceClump clump && clump.PlaceResourceClump(loc, (int)tile.X * Game1.tileSize, (int)tile.Y * Game1.tileSize, relaxed);

    #endregion

    #region crops

    /// <inheritdoc />
    public (int idx, string? stringId)? GetIdentifiers(SObject obj)
    {
        if (obj is InventoryGiantCrop crop)
        {
            string? stringID = string.IsNullOrEmpty(crop.stringID.Value) ? null : crop.stringID.Value;
            return (crop.ParentSheetIndex, stringID);
        }
        return null;
    }

    /// <inheritdoc />
    public SObject GetGiantCrop(int produceIndex, int initialStack)
    {
        return new InventoryGiantCrop(produceIndex, initialStack);
    }

    #endregion
}
