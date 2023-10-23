using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Objects;

namespace DresserMiniMenu.Framework.MiniFarmerMenuIcons;

/// <summary>
/// Selects for an item that is prismatic.
/// </summary>
internal sealed class PrismaticColorFilter : BaseColorFilter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PrismaticColorFilter"/> class.
    /// </summary>
    internal PrismaticColorFilter()
    {
    }

    /// <inheritdoc />
    internal override bool Filter(Item item)
        => item is Clothing clothing && clothing.isPrismatic.Value;

    /// <inheritdoc />
    internal override void Draw(SpriteBatch b, bool selected)
    {
        base.Draw(b, selected);
        b.Draw(
            AssetManager.Icons,
            new(this.Location.X + 4, this.Location.Y + 4),
            new Rectangle(selected ? 30 : 38, 105, 7, 7),
            Utility.GetPrismaticColor(0, 4),
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            1f);
    }
}
