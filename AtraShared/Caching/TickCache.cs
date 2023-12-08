using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

namespace AtraShared.Caching;

/// <summary>
/// Wrapper class: caches a value for approximately four ticks.
/// </summary>
/// <typeparam name="T">Type of the value.</typeparam>
/// <remarks>Constraining to just value types since reference types should run through a WeakReference.</remarks>
/// <param name="get">Function that will get the value.</param>
public struct TickCache<T>(Func<T> get)
    where T : struct
{
    private int lastTick = -1;
    private T result = default;

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <returns>Value.</returns>
    [MethodImpl(TKConstants.Hot)]
    public T GetValue()
    {
        if ((Game1.ticks & ~0b11) != this.lastTick)
        {
            this.lastTick = Game1.ticks & ~0b11;
            this.result = get();
        }
        return this.result;
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void Reset() => this.lastTick = -1;
}