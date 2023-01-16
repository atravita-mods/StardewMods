using StardewValley.TerrainFeatures;

namespace GrowableBushes.Framework;
internal static class GBUtils
{
    internal static void SetUpSourceRectForEnvironment(this Bush bush, GameLocation environment)
    {
        GameLocation oldLoc = bush.currentLocation;
        bush.currentLocation = environment;
        bush.setUpSourceRect();
        bush.currentLocation = oldLoc;
    }
}
