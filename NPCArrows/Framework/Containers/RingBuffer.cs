namespace NPCArrows.Framework.Containers;

/// <summary>
/// A ring buffer with a constant length.
/// </summary>
/// <typeparam name="T">Type of elements.</typeparam>
/// <param name="len">Length of buffer.</param>
internal sealed class ConstantRingBuffer<T>(int len)
{
    private T[] buffer = new T[len];

    private int idx = 0;

    internal int Len => len;
}
