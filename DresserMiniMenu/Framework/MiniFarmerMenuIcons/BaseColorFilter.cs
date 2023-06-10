using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DresserMiniMenu.Framework.MiniFarmerMenuIcons;

/// <summary>
/// The base class for a color filter.
/// </summary>
internal abstract class BaseColorFilter
{
    /// <summary>
    /// The size of a button.
    /// </summary>
    protected const int SIZE = 36; // px

    private Rectangle location = new(0, 0, SIZE, SIZE);

    /// <summary>
    /// Gets the location of this color filter button.
    /// </summary>
    protected Rectangle Location { get => this.location; private set => this.location = value; }

    /// <summary>
    /// Draws this color filter button.
    /// </summary>
    /// <param name="b">Sprite batch to use.</param>
    /// <param name="selected">Whether or not this element is selected.</param>
    internal virtual void Draw(SpriteBatch b, bool selected)
    {
        b.Draw(
            AssetManager.Icons,
            new(this.location.X, this.location.Y),
            new Rectangle(selected ? 29 : 37, 96, 9, 9),
            Color.White,
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            1f);
    }

    /// <summary>
    /// Whether or not a clothing item should be selected.
    /// </summary>
    /// <param name="item">The item in question.</param>
    /// <returns>True if selected, false otherwise.</returns>
    internal abstract bool Filter(Item item);

    /// <summary>
    /// If the point is within this color filter button.
    /// </summary>
    /// <param name="x">pixel x.</param>
    /// <param name="y">pixel y.</param>
    /// <returns>true.</returns>
    internal bool Contains(int x, int y) => this.Location.Contains(x, y);

    /// <summary>
    /// Reposition the button.
    /// </summary>
    /// <param name="x">x location.</param>
    /// <param name="y">y location.</param>
    internal void Reposition(int x, int y)
    {
        this.location.X = x;
        this.location.Y = y;
    }
}
