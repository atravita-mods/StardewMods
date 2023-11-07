namespace CritterRings.Legacy;

/// <summary>
/// The class used to save Ids.
/// </summary>
public sealed class LegacyIDTracker
{
    internal const string SAVEKEY = "item_ids";

    /// <summary>
    /// Gets or sets the ID of the butterfly ring.
    /// </summary>
    public int ButterflyRing { get; set; } = -1;

    /// <summary>
    /// Gets or sets the ID of the firefly ring.
    /// </summary>
    public int FireFlyRing { get; set; } = -1;

    /// <summary>
    /// Gets or sets the ID of the frog ring.
    /// </summary>
    public int FrogRing { get; set; } = -1;

    /// <summary>
    /// Gets or sets the ID of the owl ring.
    /// </summary>
    public int OwlRing { get; set; } = -1;

    /// <summary>
    /// Gets or sets the ID of the bunny ring.
    /// </summary>
    public int BunnyRing { get; set; } = -1;
}
