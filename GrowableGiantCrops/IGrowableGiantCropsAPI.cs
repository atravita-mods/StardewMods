using Microsoft.Xna.Framework;

using NetEscapades.EnumGenerators;

using StardewValley.TerrainFeatures;

#nullable enable

namespace GrowableGiantCrops;

/// <summary>
/// The api for this mod.
/// </summary>
public interface IGrowableGiantCropsAPI
{
    #region Tool methods

    /// <summary>
    /// Gets whether or not the current tool is a shovel.
    /// </summary>
    /// <param name="tool">Tool to check.</param>
    /// <returns>Whether or not the tool is a shovel.</returns>
    public bool IsShovel(Tool tool);

    /// <summary>
    /// Gets a shovel instance.
    /// </summary>
    /// <returns>A shovel.</returns>
    public Tool GetShovel();

    #endregion

    #region Generalized Placement
    /// <summary>
    /// Checks whether or an object managed by this mod.
    /// </summary>
    /// <param name="obj">Object to place.</param>
    /// <param name="loc">Game location.</param>
    /// <param name="tile">Tile to place at.</param>
    /// <param name="relaxed">Whether or not to use relaxed placement rules.</param>
    /// <returns>True if place-able.</returns>
    public bool CanPlace(StardewValley.Object obj, GameLocation loc, Vector2 tile, bool relaxed);
    #endregion

    #region Resource Clump methods

    /// <summary>
    /// Gets the InventoryResourceClump associated with the specific ResourceClumpIndex.
    /// </summary>
    /// <param name="idx">Index to use.</param>
    /// <returns>InventoryResourceClump.</returns>
    public SObject GetResourceClump(ResourceClumpIndexes idx);

    // Pintail currently does not proxy Nullable<TEnum>

    /// <summary>
    /// Gets the <see cref="ResourceClumpIndexes"/> associated with an item, or <see cref="ResourceClumpIndexes.Invalid"/>
    /// if it's not one.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <returns>A <see cref="ResourceClumpIndexes"/> (or <see cref="ResourceClumpIndexes.Invalid"/> if not applicable.).</returns>
    public ResourceClumpIndexes GetIndexOfClumpIfApplicable(StardewValley.Object obj);

    /// <summary>
    /// Checks whether or not a clump can be placed at this location.
    /// </summary>
    /// <param name="obj">Clump to place.</param>
    /// <param name="loc">Game location.</param>
    /// <param name="tile">Tile to place at.</param>
    /// <param name="relaxed">Whether or not to use relaxed placement rules.</param>
    /// <returns>True if place-able.</returns>
    public bool CanPlaceClump(StardewValley.Object obj, GameLocation loc, Vector2 tile, bool relaxed);

    /// <summary>
    /// Tries to place a clump at a specific location.
    /// </summary>
    /// <param name="obj">Clump to place.</param>
    /// <param name="loc">Game location.</param>
    /// <param name="tile">Tile to place at.</param>
    /// <param name="relaxed">Whether or not to use relaxed placement rules.</param>
    /// <returns>True if successfully placed.</returns>
    public bool TryPlaceClump(StardewValley.Object obj, GameLocation loc, Vector2 tile, bool relaxed);

    #endregion

    #region GiantCrop

    /// <summary>
    /// Gets the identifiers for an inventory giant crop, if relevant.
    /// </summary>
    /// <param name="obj">Suspected inventory giant crop.</param>
    /// <returns>the product index and the GiantCropsTweaks string id, if relevant. Null otherwise.</returns>
    public (int idx, string? stringId)? GetIdentifiers(StardewValley.Object obj);

    public StardewValley.Object GetGiantCrop(int produceIndex, int initialStack);
    #endregion
}

/// <summary>
/// The enum used for resource clumps.
/// Do not copy the [EnumExtensions] attribute, that is used for internal source generation.
/// </summary>
[EnumExtensions]
public enum ResourceClumpIndexes
{
    Stump = 600,
    HollowLog = 602,
    Meteorite = 622,
    Boulder = 672,
    MineRockOne = 752,
    MineRockTwo = 754,
    MineRockThree = 756,
    MineRockFour = 758,

    // usually would have just used null,
    // but Pintail can't proxy Nullable<TEnum> right now.

    /// <summary>
    /// Represents an invalid ResourceClumpIndex
    /// </summary>
    Invalid = -999,
}

/// <summary>
/// The enum used for different grass types.
/// Do not copy the [EnumExtensions] attribute, that is used for internal source generation.
/// </summary>
[EnumExtensions]
public enum GrassIndexes
{
    Spring = Grass.springGrass,
    Cave = Grass.caveGrass,
    Forst = Grass.frostGrass,
    Lava = Grass.lavaGrass,
    CaveTwo = Grass.caveGrass2,
    Cobweb = Grass.cobweb,

    // usually would have just used null,
    // but Pintail can't proxy Nullable<TEnum> right now.

    /// <summary>
    /// Represents an invalid GrassIndex
    /// </summary>
    Invalid = -999,
}

/// <summary>
/// The enum used for different tree types.
/// Do not copy the [EnumExtensions] attribute, that is used for internal source generation.
/// </summary>
[EnumExtensions]
public enum TreeIndexes
{
    Maple = Tree.bushyTree,
    Oak = Tree.leafyTree,
    Pine = Tree.pineTree,
    Palm = Tree.palmTree,
    BigPalm = Tree.palmTree2,
    Mahogany = Tree.mahoganyTree,
    Mushroom = Tree.mushroomTree,

    // usually would have just used null,
    // but Pintail can't proxy Nullable<TEnum> right now.

    /// <summary>
    /// Represents an invalid TreeIndex
    /// </summary>
    Invalid = -999,
}