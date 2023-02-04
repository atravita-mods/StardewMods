using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;

using StardewValley.TerrainFeatures;

namespace GrowableBushes.Framework;

/// <inheritdoc />
public sealed class GrowableBushesAPI : IGrowableBushesAPI
{
    /// <inheritdoc />
    public SObject GetBush(BushSizes size) => new InventoryBush(size, 1);

    /// <inheritdoc />
    public BushSizes GetSizeOfBushIfApplicable(SObject obj)
    {
        if (obj is InventoryBush bush && BushSizesExtensions.IsDefined((BushSizes)bush.ParentSheetIndex))
        {
            return (BushSizes)bush.ParentSheetIndex;
        }
        return BushSizes.Invalid;
    }

    /// <inheritdoc />
    public BushSizes CanPickUpBush(GameLocation loc, Vector2 tile, bool placedOnly = false)
    {
        if (loc.largeTerrainFeatures is null)
        {
            return BushSizes.Invalid;
        }

        LargeTerrainFeature? feat = loc.getLargeTerrainFeatureAt((int)tile.X, (int)tile.Y);
        if (feat is Bush bush)
        {
            return this.CanPickUpBush(bush, placedOnly);
        }
        return BushSizes.Invalid;
    }

    public BushSizes CanPickUpBush(Bush bush, bool placedOnly = false)
    {
        BushSizes metaData = bush.modData.GetEnum(InventoryBush.BushModData, BushSizes.Invalid);

        if (placedOnly)
        {
            return metaData;
        }

        BushSizes size = bush.ToBushSize();
        return size == BushSizes.Walnut ? BushSizes.Invalid : size;
    }

    /// <inheritdoc />
    public SObject? TryPickUpBush(GameLocation loc, Vector2 tile, bool placedOnly = false)
    {
        if (loc.largeTerrainFeatures is not null)
        {
            Rectangle tileRect = new((int)tile.X * 64, (int)tile.Y * 64, 64, 64);
            for (int i = loc.largeTerrainFeatures.Count - 1; i >= 0; i--)
            {
                if (loc.largeTerrainFeatures[i] is Bush bush && bush.getBoundingBox().Intersects(tileRect))
                {
                    BushSizes size = this.CanPickUpBush(bush, placedOnly);
                    if (size != BushSizes.Invalid)
                    {
                        InventoryBush.BushShakeMethod(bush, bush.currentTileLocation, true);
                        loc.largeTerrainFeatures.RemoveAt(i);
                        return (InventoryBush?)new InventoryBush(size, 1);
                    }
                }
            }
        }
        return null;
    }

    /// <inheritdoc />
    public bool CanPlaceBush(SObject obj, GameLocation loc, Vector2 tile, bool relaxed)
        => obj is InventoryBush bush && bush.CanPlace(loc, tile, relaxed);

    /// <inheritdoc />
    public bool TryPlaceBush(SObject obj, GameLocation loc, Vector2 tile, bool relaxed)
        => obj is InventoryBush bush && bush.PlaceBush(loc, (int)(tile.X * Game1.tileSize), (int)(tile.Y * Game1.tileSize), relaxed);
}
