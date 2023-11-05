using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using StardewValley.Delegates;

namespace AtraCore.Framework.EventPreconditions;

/// <summary>
/// Handles event preconditions for the player's dating status.
/// </summary>
internal static class PlayerRelationshipPreconditions
{
    /// <inheritdoc cref="EventPreconditionDelegate"/>
    internal static bool PlayerRelationshipStatus(GameLocation location, string eventId, string[] split)
    {
        if (split.Length < 2 || !FriendshipEnumExtensions.TryParse(split[1], out FriendshipEnum friendshipEnum, ignoreCase: true))
        {
            ModEntry.ModMonitor.Log($"Checking event preconditions for '{eventId}' failed: unknown value for relationship: '{split[1]}'", LogLevel.Warn);
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
                return Game1.player.IsMarried();
            }
            default:
            {
                ModEntry.ModMonitor.Log($"Checking event preconditions for '{eventId}' failed: invalid value for relationship: '{split[1]}'", LogLevel.Warn);
                return false;
            }
        }
    }
}
