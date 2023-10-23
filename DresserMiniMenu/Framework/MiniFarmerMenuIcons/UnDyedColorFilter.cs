using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Objects;

namespace DresserMiniMenu.Framework.MiniFarmerMenuIcons;

/// <summary>
/// Filters for items that aren't dyed.
/// </summary>
internal class UnDyedColorFilter : BaseColorFilter
{
    /// <inheritdoc />
    internal override bool Filter(Item item) => item is not Clothing clothing || (!clothing.dyeable.Value && !clothing.isPrismatic.Value);

    /// <inheritdoc />
    internal override void Draw(SpriteBatch b, bool selected)
    {
        base.Draw(b, selected);
        b.Draw(
            AssetManager.Icons,
            new(this.Location.X + 4, this.Location.Y + 4),
            new Rectangle(46, 97, 7, 7),
            selected ? Color.Gray * 0.7f : Color.White,
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            1f);
    }
}
