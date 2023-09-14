using AtraShared.ConstantsAndEnums;

namespace AtraCore.Framework.EventPreconditions;

/// <summary>
/// Handles event preconditions for the player's dating status.
/// </summary>
internal static class PlayerRelationshipPreconditions
{
    internal static bool PlayerRelationshipStatus(GameLocation location, string event_id, string[] split)
    {
        if (FriendshipEnumExtensions.TryParse(split[1], out FriendshipEnum friendshipEnum))
        {
            ModEntry.ModMonitor.Log($"Checking event preconditions for '{event_id}' failed: unknown value for relationship: '{split[1]}'", LogLevel.Warn);
            return false;
        }

        switch (friendshipEnum)
        {
            case FriendshipEnum.Dating:
            {
                foreach (Friendship? friendship in Game1.player.friendshipData.Values)
                {
                    if (friendship.Status == FriendshipStatus.Dating)
                    {
                        return true;
                    }
                }
                return false;
            }
            case FriendshipEnum.Engaged:
            {
                return Game1.player.isEngaged();
            }
            case FriendshipEnum.Married:
            {
                return Game1.player.isMarried();
            }
            default:
            {
                ModEntry.ModMonitor.Log($"Checking event preconditions for '{event_id}' failed: invalid value for relationship: '{split[1]}'", LogLevel.Warn);
                return false;
            }
        }
    }
}
