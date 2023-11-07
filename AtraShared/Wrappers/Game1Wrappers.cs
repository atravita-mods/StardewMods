using StardewValley.GameData.Objects;

namespace AtraShared.Wrappers;

public static class Game1Wrappers
{
    /// <summary>
    /// This exists solely because for a split second initializing a new player, <see cref="Game1.objectData" /> can be null.
    /// Let it be known I am very mad.
    /// </summary>
    public static IDictionary<string, ObjectData> ObjectData => Game1.objectData ??= Game1.content.Load<Dictionary<string, ObjectData>>("Data/Objects");
}
