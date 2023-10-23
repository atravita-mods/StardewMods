namespace AtraCore;

/// <summary>
/// A class that holds instances for reuse.
/// </summary>
public static class Singletons
{
    private static readonly ThreadLocal<Random> _random = new(valueFactory: () => new());

    /// <summary>
    /// Gets a thread safe random.
    /// </summary>
    public static Random Random => _random.Value!;
}
