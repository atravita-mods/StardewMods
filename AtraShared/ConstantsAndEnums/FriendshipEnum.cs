using NetEscapades.EnumGenerators;

namespace AtraShared.ConstantsAndEnums;

/// <summary>
/// An enum referring to the friendship status of an NPC.
/// </summary>
[EnumExtensions]
public enum FriendshipEnum
{
    /// <summary>
    /// The NPC has met the player.
    /// </summary>
    Friendly = FriendshipStatus.Friendly,

    /// <summary>
    /// The NPC is dating the player.
    /// </summary>
    Dating = FriendshipStatus.Dating,

    /// <summary>
    /// The NPC is engaged to the player.
    /// </summary>
    Engaged = FriendshipStatus.Engaged,

    /// <summary>
    /// The NPC is married to the player.
    /// </summary>
    Married = FriendshipStatus.Married,

    /// <summary>
    /// The NPC is divorced from the player.
    /// </summary>
    Divorced = FriendshipStatus.Divorced,

    /// <summary>
    /// The NPC is a roommate.
    /// </summary>
    Roommate,

    /// <summary>
    /// The NPC hasn't met the player.
    /// </summary>
    Unmet,
}