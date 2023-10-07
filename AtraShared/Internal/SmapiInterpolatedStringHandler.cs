// Ignore Spelling: pred

namespace AtraShared.Internal;

using System.Runtime.CompilerServices;

[InterpolatedStringHandler]
public ref struct SmapiInterpolatedStringHandler
{
    private DefaultInterpolatedStringHandler _handler;

    public SmapiInterpolatedStringHandler(int literateLength, int formattedCount, IMonitor monitor, out bool handlerIsValid)
    {
        if (monitor.IsVerbose)
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