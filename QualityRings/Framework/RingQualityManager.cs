using System.Runtime.CompilerServices;
using Netcode;
using StardewValley.Objects;

namespace QualityRings.Framework;

/// <summary>
/// Helps manage the quality of rings.
/// </summary>
internal static class RingQualityManager
{
    private static readonly ConditionalWeakTable<Ring, NetInt> QualityMap = new();

    // Fake properties - register with spacecore.

    /// <summary>
    /// Gets the quality for a ring.
    /// </summary>
    /// <param name="ring">Ring in question.</param>
    /// <returns>quality of the ring.</returns>
    internal static NetInt get_Quality(Ring ring)
        => QualityMap.TryGetValue(ring, out NetInt? val) ? val : new NetInt(0);

    /// <summary>
    /// Sets the quality for a ring.
    /// </summary>
    /// <param name="ring">Ring in question.</param>
    /// <param name="quality">quality of the ring.</param>
    internal static void set_Quality(Ring ring, NetInt quality)
        => QualityMap.AddOrUpdate(ring, quality);
}
