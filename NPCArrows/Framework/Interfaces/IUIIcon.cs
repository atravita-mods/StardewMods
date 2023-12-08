using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal interface IUIIcon : IDisposable
{
    internal void Draw(SpriteBatch b);

    internal void Reposition(Point p);

    internal void TryHover(int x, int y);

    internal string? GetHoverText();

    internal bool IsDisposed { get; }
}