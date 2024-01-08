using AtraBase.Toolkit;
using AtraBase.Toolkit.Extensions;

namespace AtraShared.Utils;

/// <summary>
/// Utilities for generating good randoms.
/// </summary>
public static class RandomUtils
{
    /// <summary>
    /// Gets a random seeded by the days played, the unique ID, and another initial factor.
    /// </summary>
    /// <param name="dayFactor">seeding factor.</param>
    /// <param name="initial">seeding factor but a string.</param>
    /// <remarks>Comes prewarmed.</remarks>
    /// <returns>A seeded random.</returns>
    public static Random GetSeededRandom(int dayFactor, string initial)
        => GetSeededRandom(dayFactor, Game1.hash.GetDeterministicHashCode(initial));

    /// <summary>
    /// Gets a random seeded by the days played, the unique ID, and another initial factor.
    /// </summary>
    /// <param name="dayFactor">seeding factor.</param>
    /// <param name="initial">another seeding factor.</param>
    /// <remarks>Comes prewarmed.</remarks>
    /// <returns>A seeded random.</returns>
    public static Random GetSeededRandom(int dayFactor, int initial)
    {
        unchecked
        {
            Random random = new((int)(Game1.uniqueIDForThisGame + (ulong)(dayFactor * Game1.stats.DaysPlayed) ^ (ulong)initial));
            random.PreWarm();
            return random;
        }
    }
}
