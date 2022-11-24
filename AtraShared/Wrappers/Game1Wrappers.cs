namespace AtraShared.Wrappers;

public static class Game1Wrappers
{
    /// <summary>
    /// This exists solely because for a split second initializing a new player, <see cref="Game1.objectInformation"/ > can be null.
    /// Let it be known I am very mad.
    /// </summary>
    public static IDictionary<int, string> ObjectInfo => Game1.objectInformation ??= Game1.content.Load<Dictionary<int, string>>("Data/ObjectInformation");
}
