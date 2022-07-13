using Microsoft.Xna.Framework;

namespace AtraShared.Integrations.GMCMAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GMCMDefaultColorAttribute : Attribute
{
    public GMCMDefaultColorAttribute(byte R, byte G, byte B, byte A)
    {
        this.R = R;
        this.G = G;
        this.B = B;
        this.A = A;
    }

    internal byte R { get; init; }

    internal byte G { get; init; }

    internal byte B { get; init; }

    internal byte A { get; init; }
}
