using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Menus;

namespace CatGiftsRedux.Framework;

/// <summary>
/// A custom subclass of the Hudmessage to draw in the pet's head and the item.
/// </summary>
internal sealed class PetHudMessage : HUDMessage
{
    private Item spawnedItem;

    /// <summary>
    /// Initializes a new instance of the <see cref="PetHudMessage"/> class.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="color"></param>
    /// <param name="timeLeft"></param>
    /// <param name="fadeIn"></param>
    /// <param name="spawnedItem"></param>
    public PetHudMessage(string message, Color color, float timeLeft, bool fadeIn, Item spawnedItem)
        : base(message, color, timeLeft, fadeIn)
    {
        this.spawnedItem = spawnedItem;
    }

    public override void draw(SpriteBatch b, int i)
    {
        Rectangle tsarea = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();
        Vector2 itemBoxPosition = new(tsarea.Left + 16, tsarea.Bottom - ((i + 1) * 64 * 7 / 4) - 64);
        if (Game1.isOutdoorMapSmallerThanViewport())
        {
            itemBoxPosition.X = Math.Max(tsarea.Left + 16, -Game1.uiViewport.X + 16);
        }
        if (Game1.uiViewport.Width < 1400)
        {
            itemBoxPosition.Y -= 48f;
        }

        // draws the left boxen.
        b.Draw(
            Game1.mouseCursors,
            itemBoxPosition,
            new Rectangle(293, 360, 26, 24),
            Color.White * this.transparency,
            0f,
            Vector2.Zero,
            4f,
            SpriteEffects.None,
            1f);

        // draws the bit the message sits in.
        float messageWidth = ModEntry.StringUtils.MeasureWord(Game1.smallFont, this.message);
        b.Draw(
            Game1.mouseCursors,
            new Vector2(itemBoxPosition.X + 104f, itemBoxPosition.Y),
            new Rectangle(319, 360, 1, 24),
            Color.White * this.transparency,
            0f,
            Vector2.Zero,
            new Vector2(messageWidth, 4f),
            SpriteEffects.None,
            1f);

        // draw the right side of the box.
        b.Draw(
            Game1.mouseCursors,
            new Vector2(itemBoxPosition.X + 104f + messageWidth, itemBoxPosition.Y),
            new Rectangle(323, 360, 6, 24),
            Color.White * this.transparency,
            0f,
            Vector2.Zero,
            4f,
            SpriteEffects.None,
            1f);
        itemBoxPosition.X += 16f;
        itemBoxPosition.Y += 16f;
        b.Draw(
            Game1.mouseCursors,
            itemBoxPosition + (new Vector2(8f, 8f) * 4f),
            new Rectangle(160 + ((!Game1.player.catPerson) ? 48 : 0) + Game1.player.whichPetBreed * 16, 208, 16, 16),
            Color.White * this.transparency,
            0f,
            new Vector2(8f, 8f),
            4f + Math.Max(0f, (this.timeLeft - 3000f) / 900f),
            SpriteEffects.None,
            1f);

        itemBoxPosition.X += 51f;
        itemBoxPosition.Y += 51f;
        itemBoxPosition.X += 32f;
        itemBoxPosition.Y -= 33f;
        Utility.drawTextWithShadow(
            b,
            this.message,
            Game1.smallFont,
            itemBoxPosition,
            Game1.textColor * this.transparency,
            1f,
            1f,
            -1,
            -1,
            this.transparency);
    }
}
