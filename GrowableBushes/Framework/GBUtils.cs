using StardewValley.TerrainFeatures;

namespace GrowableBushes.Framework;

/// <summary>
/// The utility class for this mod.
/// </summary>
internal static class GBUtils
{
    /// <summary>
    /// Sets up the source rect given the given gamelocation.
    /// </summary>
    /// <param name="bush">Bush.</param>
    /// <param name="environment">GameLocation to use.</param>
    internal static void SetUpSourceRectForEnvironment(this Bush bush, GameLocation environment)
    {
        environment ??= bush.currentLocation;
        GameLocation oldLoc = bush.currentLocation;
        bush.currentLocation = environment;
        bush.setUpSourceRect();
        bush.currentLocation = oldLoc;
    }
}
