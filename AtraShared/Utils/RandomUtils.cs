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
        => GetSeededRandom(dayFactor, initial.GetStableHashCode());

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

    // the game sometimes uses floats for ids
    // that's a terrible idea, but we now need to deal with it.
    // this lets me get a random float for an id in a way that's safe, covers as much of the
    // possible range as possible, and doesn't involve anything too stupid.
    public static float GetRandomFloatId(int initialValue, Random? random = null)
    {
        // usually we can get away with using something like the sync key as the initial value
        // and avoid using Random, which can be expensive.
        float result = BitConverter.Int32BitsToSingle(initialValue);
        if (!float.IsNaN(result))
        {
            return result;
        }

        random ??= Random.Shared;
        Span<byte> buffer = stackalloc byte[4];
        do
        {
            random.NextBytes(buffer);
            result = BitConverter.ToSingle(buffer);
        }
        while (float.IsNaN(result));

        return result;
    }
}
