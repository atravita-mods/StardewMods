using Microsoft.Xna.Framework;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extensions on Farmer.
/// </summary>
public static class FarmerExtensions
{
    /// <summary>
    /// Faces the farmer towards the pixel location.
    /// </summary>
    /// <param name="farmer">Farmer to turn.</param>
    /// <param name="nonTilePosition">Pixel to turn to.</param>
    public static void FaceFarmerTowardsPosition(this Farmer farmer, Vector2 nonTilePosition)
    {
        Vector2 delta = nonTilePosition - farmer.Position;
        if (Math.Abs(delta.X) < Math.Abs(delta.Y))
        {
            farmer.FacingDirection = delta.Y <= 0 ? Game1.up : Game1.down;
        }
        else
        {
            farmer.FacingDirection = delta.X <= 0 ? Game1.left : Game1.right;
        }
    }
}