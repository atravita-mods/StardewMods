using NetEscapades.EnumGenerators;

using StardewValley.TerrainFeatures;

namespace GrowableBushes.Framework;

/// <summary>
/// Valid bush sizes.
/// </summary>
[EnumExtensions]
public enum BushSizes
{
    // base game bush sizes
    Small = Bush.smallBush,
    Medium = Bush.mediumBush,
    Large = Bush.largeBush,
}

internal static class BushSizesExtraExtensions
{
    internal static int ToStardewBush(this BushSizes sizes)
        => sizes switch
        {
            _ when BushSizesExtensions.IsDefined(sizes) => (int)sizes,
            _ => Bush.smallBush,
        };
}