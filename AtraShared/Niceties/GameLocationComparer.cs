// Ignore Spelling: lhs rhs

namespace AtraShared.Niceties;

/// <summary>
/// A comparer to use for game locations that compares by the name.
/// </summary>
public sealed class GameLocationNameComparer : EqualityComparer<GameLocation>
{
    private static readonly GameLocationNameComparer instance = new();

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static GameLocationNameComparer Instance => instance;

    /// <inheritdoc />
    public override bool Equals(GameLocation? lhs, GameLocation? rhs)
    {
        if (lhs is null)
        {
            return rhs is null;
        }
        if (rhs is null)
        {
            return false;
        }
        return ReferenceEquals(lhs, rhs) || lhs.Name == rhs.Name;
    }

    /// <inheritdoc />
    public override int GetHashCode([DisallowNull] GameLocation location) => location.Name.GetHashCode();
}

/// <summary>
/// A comparer to use for unique names of locations.
/// </summary>
public sealed class GameLocationUniqueNameComparer : EqualityComparer<GameLocation>
{
    private static readonly GameLocationUniqueNameComparer instance = new();

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static GameLocationUniqueNameComparer Instance => instance;

    /// <inheritdoc />
    public override bool Equals(GameLocation? lhs, GameLocation? rhs)
    {
        if (lhs is null)
        {
            return rhs is null;
        }
        if (rhs is null)
        {
            return false;
        }
        return ReferenceEquals(lhs, rhs) || lhs.NameOrUniqueName == rhs.NameOrUniqueName;
    }

    /// <inheritdoc />
    public override int GetHashCode([DisallowNull] GameLocation location) => location.NameOrUniqueName.GetHashCode();
}