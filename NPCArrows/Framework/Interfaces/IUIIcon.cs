using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// An icon to present under the clock.
/// </summary>
internal interface IUIIcon : IDisposable
{
    /// <summary>
    /// Draws the icon.
    /// </summary>
    /// <param name="b">Spritebatch to use.</param>
    internal void Draw(SpriteBatch b);

    /// <summary>
    /// Repositions the icons to this location.
    /// </summary>
    /// <param name="p"></param>
    internal void Reposition(Point p);

    /// <summary>
    /// Checks to see if the icon can be hovered.
    /// </summary>
    /// <param name="x">pixel x of mouse.</param>
    /// <param name="y">pixel y of mouse.</param>
    internal void TryHover(int x, int y);

    /// <summary>
    /// Gets the hovertext associated with this UI element.
    /// </summary>
    /// <returns>hovertext, or null if none.</returns>
    internal string? GetHoverText();

    /// <summary>
    /// Gets a value indicating whether this instance is disposed.
    /// </summary>
    internal bool IsDisposed { get; }
}