using StardewValley.TerrainFeatures;

namespace GrowableBushes.Framework;

/// <summary>
/// Extension methods for BushSizes.
/// </summary>
internal static class BushSizesExtraExtensions
{
    /// <summary>
    /// Gets the <see cref="Bush.size"/> for the BushSizes.
    /// </summary>
    /// <param name="sizes">BushSize.</param>
    /// <returns>int size.</returns>
    internal static int ToStardewBush(this BushSizes sizes)
        => sizes switch
        {
            BushSizes.SmallAlt => Bush.smallBush,
            BushSizes.Town => Bush.mediumBush,
            BushSizes.TownLarge => Bush.largeBush,
            BushSizes.Harvested => Bush.walnutBush,
            _ when BushSizesExtensions.IsDefined(sizes) => (int)sizes,
            _ => Bush.smallBush,
        };

    /// <summary>
    /// Get the width (in tiles) of a specific BushSize.
    /// </summary>
    /// <param name="sizes">size.</param>
    /// <returns>width.</returns>
    internal static int GetWidth(this BushSizes sizes)
        => sizes switch
        {
            BushSizes.Small or BushSizes.SmallAlt => 1,
            BushSizes.Large or BushSizes.TownLarge => 3,
            _ => 2
        };
}