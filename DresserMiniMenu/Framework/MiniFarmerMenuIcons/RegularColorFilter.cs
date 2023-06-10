using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Objects;

namespace DresserMiniMenu.Framework.MiniFarmerMenuIcons;

/// <summary>
/// Represents a normal color filter.
/// </summary>
internal sealed class RegularColorFilter : BaseColorFilter
{
    private readonly Color color;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegularColorFilter"/> class.
    /// </summary>
    /// <param name="color">The color the color filter refers to.</param>
    public RegularColorFilter(Color color) => this.color = color;

    /// <inheritdoc />
    internal override bool Filter(Item item)
    {
        if (item is not Clothing clothing || !clothing.dyeable.Value || clothing.isPrismatic.Value)
        {
            return false;
        }
        Color color = clothing.clothesColor.Value;

        byte min = Math.Min(Math.Min(color.R, color.B), color.G);
        if (min > 200)
        {
            return this.color == Color.White;
        }

        byte max = Math.Max(Math.Max(color.R, color.B), color.G);
        if (max < 50)
        {
            return this.color == Color.Black;
        }

        if (max - min < 35)
        {
            return this.color == Color.Gray;
        }

        byte mid = max;
        if (color.R == color.B)
        {
            mid = color.B;
        }
        else if (color.R == color.G)
        {
            mid = color.G;
        }
        else if (color.B == color.G)
        {
            mid = color.G;
        }
        else if (color.R != max && color.R != min)
        {
            mid = color.R;
        }
        else if (color.G != max && color.G != min)
        {
            mid = color.G;
        }
        else if (color.B != max && color.B != min)
        {
            mid = color.B;
        }

        if (this.color == Color.Red)
        {
            return max == color.R && (max - mid) > (mid - min);
        }
        else if (this.color == Color.Yellow)
        {
            return min == color.B && (max - mid) <= (mid - min);
        }
        else if (this.color == Color.Green)
        {
            return max == color.G && (max - mid) > (mid - min);
        }
        else if (this.color == Color.Blue)
        {
            return (max == color.B && (max - mid) > (mid - min))
                || (min == color.R && (max - mid) <= (mid - min));
        }
        else if (this.color == Color.Purple)
        {
            return min == color.G && (max - mid) <= (mid - min);
        }

        return false;
    }

    /// <inheritdoc />
    internal override void Draw(SpriteBatch b, bool selected)
    {
        base.Draw(b, selected);
        b.Draw(
            AssetManager.Icons,
            new(this.Location.X + 4, this.Location.Y + 4),
            new Rectangle(selected ? 30 : 38, 105, 7, 7),
            this.color,
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            1f);
    }
}
