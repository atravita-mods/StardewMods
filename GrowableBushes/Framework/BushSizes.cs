using NetEscapades.EnumGenerators;

using StardewValley.TerrainFeatures;

namespace GrowableBushes.Framework;

internal static class BushSizesExtraExtensions
{
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

    internal static int GetWidth(this BushSizes sizes)
        => sizes switch
        {
            BushSizes.Small or BushSizes.SmallAlt => 1,
            BushSizes.Large or BushSizes.TownLarge => 3,
            _ => 2
        };
}