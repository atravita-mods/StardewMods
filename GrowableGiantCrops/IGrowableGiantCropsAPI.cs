using NetEscapades.EnumGenerators;

#nullable enable

namespace GrowableGiantCrops;

/// <summary>
/// The api for this mod.
/// </summary>
public interface IGrowableGiantCropsAPI
{
    /// <summary>
    /// Gets whether or not the current tool is a shovel.
    /// </summary>
    /// <param name="tool">Tool to check.</param>
    /// <returns>Whether or not the tool is a shovel.</returns>
    public bool IsShovel(Tool tool);

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