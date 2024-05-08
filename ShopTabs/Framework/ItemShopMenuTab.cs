using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using static StardewValley.Menus.ShopMenu;

namespace ShopTabs.Framework;

/// <summary>
/// A shop tab that draws some 16*16 item.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ItemShopMenuTab"/> class.
/// </remarks>
/// <param name="texture">The texture of the item to draw.</param>
/// <param name="sourceRect">The source rectangle of the item.</param>
internal class ItemShopMenuTab(Texture2D texture, Rectangle sourceRect)
    : ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), AssetEditor.EmptyTab, new Rectangle(0,0,16,16), Game1.pixelZoom, false)
{
    private readonly Texture2D itemTexture = texture;
    private readonly Rectangle itemRectangle = sourceRect;

    /// <inheritdoc />
    public override void draw(SpriteBatch b)
    {
        if (this.visible)
        {
            this.draw(b, Color.White, 0.86f + (this.bounds.Y / 20000f));
            b.Draw(
            this.itemTexture,
            new Vector2(this.bounds.X + 24, this.bounds.Y + 16),
            this.itemRectangle,
            Color.White,
            rotation: 0f,
            origin: Vector2.Zero,
            scale: 2f,
            SpriteEffects.None,
            0.87f);
        }
    }
}