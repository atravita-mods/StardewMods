using Microsoft.Xna.Framework;

namespace AtraShared.Utils.Extensions;

internal static class FarmerExtensions
{
    internal static void FaceFarmerTowardsPosition(this Farmer farmer, Vector2 nonTilePosition)
    {
        Vector2 delta = nonTilePosition - farmer.Position;
        if (Math.Abs(delta.X) < Math.Abs(delta.Y))
        {
            farmer.FacingDirection = delta.Y <= 0 ? Game1.up : Game1.down;
            return;
        }
        else
        {
            farmer.FacingDirection = delta.X <= 0 ? Game1.left : Game1.right;
        }
    }
}