using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

using NetEscapades.EnumGenerators;

using StardewValley.TerrainFeatures;

namespace GrowableBushes;

/// <summary>
/// The API for Growable Bushes.
/// You will also need the enum BushSizes.cs
/// </summary>
public interface IGrowableBushesAPI
{

    /// <summary>
    /// Checks whether or not an InventoryBush can be placed.
    /// </summary>
    /// <param name="obj">StardewValley.Object to place.</param>
    /// <param name="loc">GameLocation to place at.</param>
    /// <param name="tile">Tile to place at.</param>
    /// <returns>True if the SObject is an InventoryBush and can be placed, false otherwise.</returns>
    public bool CanPlaceBush(StardewValley.Object obj, GameLocation loc, Vector2 tile);

    /// <summary>
    /// Called to place an InventoryBush at a specific location.
    /// </summary>
    /// <param name="obj">StardewValley.Object to place.</param>
    /// <param name="loc">GameLocation to place at.</param>
    /// <param name="tile">Which tile to place at.</param>
    /// <returns>True if the SObject is an InventoryBush and was successfully placed, false otherwise.</returns>
    /// <remarks>Does not handle inventory management.</remarks>
    public bool TryPlaceBush(StardewValley.Object obj, GameLocation loc, Vector2 tile);

    /// <summary>
    /// Gets the InventoryBush associated with a specific size.
    /// </summary>
    /// <param name="size">The size to get.</param>
    /// <returns>An InventoryBush.</returns>
    public StardewValley.Object GetBush(BushSizes size);

    /// <summary>
    /// Gets the size associated with this InventoryBush, or null if it's not an InventoryBush.
    /// </summary>
    /// <param name="obj">Object to check.</param>
    /// <returns>Size if applicable, null if not.</returns>
    public BushSizes? GetSizeOfBushIfApplicable(StardewValley.Object obj);
}

/// <summary>
/// Valid bush sizes. Do NOT copy the EnumExtensions attribute,
/// that's used for internal sourcegen.
/// </summary>
[EnumExtensions]
public enum BushSizes
{
    // base game bush sizes
    Small = Bush.smallBush,
    Medium = Bush.mediumBush,
    Large = Bush.largeBush,

    Harvested = Bush.walnutBush - 1,
    Walnut = Bush.walnutBush,

    // weird sizes
    SmallAlt = 7,
    Town = 8,
    TownLarge = 9,
}