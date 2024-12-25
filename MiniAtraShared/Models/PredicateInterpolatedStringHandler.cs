namespace MiniAtraShared.Models;

#if NET6_0_OR_GREATER

using System.Runtime.CompilerServices;

/// <summary>
/// An interpolated string handler that checks a predicate before interpreting.
/// </summary>
[InterpolatedStringHandler]
public ref struct PredicateInterpolatedStringHandler
{
    private DefaultInterpolatedStringHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="PredicateInterpolatedStringHandler"/> struct.
    /// </summary>
    /// <param name="literateLength"></param>
    /// <param name="formattedCount"></param>
    /// <param name="predicate"></param>
    /// <param name="handlerIsValid"></param>
    public PredicateInterpolatedStringHandler(int literateLength, int formattedCount, bool predicate, out bool handlerIsValid)
    {
        if (!predicate)
        {
            handlerIsValid = false;
            this._handler = default;
            return;
        }

        handlerIsValid = true;
        this._handler = new DefaultInterpolatedStringHandler(literateLength, formattedCount);
    }

    public void AppendLiteral(string s) => this._handler.AppendLiteral(s);

    public void AppendFormatted<T>(T t) => this._handler.AppendFormatted(t);

    public override string ToString() => this._handler.ToStringAndClear();
}

#endif