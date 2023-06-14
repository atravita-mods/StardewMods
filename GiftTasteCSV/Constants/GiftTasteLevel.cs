using NetEscapades.EnumGenerators;

namespace GiftTasteCSV.Constants;

/// <summary>
/// Maps the name of a gift taste to the level.
/// </summary>
[EnumExtensions]
public enum GiftTasteLevel
{
    Love = 1,
    Like = 3,
    Dislike = 5,
    Hate = 7,
    Neutral = 9,
}
