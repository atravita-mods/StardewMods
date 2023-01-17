using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

namespace GrowableBushes.Framework;

/// <inheritdoc />
public sealed class GrowableBushesAPI : IGrowableBushesAPI
{
    /// <inheritdoc />
    public bool CanPlaceBush(SObject obj, GameLocation loc, Vector2 tile) => obj is InventoryBush bush && bush.canBePlacedHere(loc, tile);

    /// <inheritdoc />
    public SObject GetBush(BushSizes size) => new InventoryBush(size, 1);

    /// <inheritdoc />
    public BushSizes? GetSizeOfBushIfApplicable(SObject obj)
    {
        if (obj is InventoryBush bush && BushSizesExtensions.IsDefined((BushSizes)bush.ParentSheetIndex))
        {

            return (BushSizes)bush.ParentSheetIndex;

        }
        return null;
    }

    /// <inheritdoc />
    public bool TryPlaceBush(SObject obj, GameLocation loc, Vector2 tile)
        => obj is InventoryBush bush && bush.placementAction(loc, (int)(tile.X * Game1.tileSize), (int)(tile.Y * Game1.tileSize), Game1.player);
}
