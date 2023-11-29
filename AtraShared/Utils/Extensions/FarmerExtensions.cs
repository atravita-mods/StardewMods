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

    /// <summary>
    /// Gets a facing direction corresponding to the direction an Farmer is facing.
    /// </summary>
    /// <param name="farmer">Farmer in question.</param>
    /// <returns>Direction they're facing.</returns>
    public static Vector2 GetFacingDirection(this Farmer farmer)
    {
        return farmer.facingDirection.Get() switch
        {
            Game1.up => -Vector2.UnitY,
            Game1.down => Vector2.UnitY,
            Game1.left => -Vector2.UnitX,
            Game1.right => Vector2.UnitX,
            _ => Vector2.Zero,
        };
    }

    /// <summary>
    /// Basically a copy of the old IsMarried, just moved to an extension method.
    /// </summary>
    /// <param name="farmer">The farmer to check.</param>
    /// <returns>Whether or not the farmer is married.</returns>
    public static bool IsMarried(this Farmer farmer)
    {
        if (farmer.team.IsMarried(farmer.UniqueMultiplayerID))
        {
            return true;
        }
        return farmer.spouse is { } spouse && farmer.friendshipData.TryGetValue(spouse, out Friendship? friendship)
            && friendship.IsMarried() && !friendship.RoommateMarriage;
    }
}