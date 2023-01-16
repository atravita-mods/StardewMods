using NetEscapades.EnumGenerators;

using StardewValley.TerrainFeatures;

namespace GrowableBushes.Framework;

/// <summary>
/// Valid bush sizes.
/// </summary>
[EnumExtensions]
internal enum BushSizes
{
    // base game bush sizes

    Small = Bush.smallBush,
    Medium = Bush.mediumBush,
    Large = Bush.largeBush,

    AlternativeSmall = 3,
    Town = 4,
}

internal static class BushSizesExtraExtensions
{
    internal static int ToStardewBush(this BushSizes sizes)
        => sizes switch
        {
            BushSizes.AlternativeSmall => Bush.smallBush,
            BushSizes.Town => Bush.mediumBush,
            _ when BushSizesExtensions.IsDefined(sizes) => (int)sizes,
            _ => Bush.smallBush,
        };
}