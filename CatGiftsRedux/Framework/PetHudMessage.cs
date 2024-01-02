using System.Runtime.CompilerServices;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Characters;
using StardewValley.Extensions;

namespace CatGiftsRedux.Framework;

/// <summary>
/// A custom subclass of the HudMessage to draw in the pet's head and the item.
/// </summary>
internal sealed class PetHudMessage : HUDMessage
{
    private readonly Item spawnedItem;
    private readonly Texture2D? petTex;
    private readonly Rectangle petRect;
    private readonly float messageWidth;

    /// <summary>
    /// Initializes a new instance of the <see cref="PetHudMessage"/> class.
    /// </summary>
    /// <param name="message">The message to include.</param>
    /// <param name="timeLeft">How much time the boxen should hang around for.</param>
    /// <param name="fadeIn">Whether or not the boxen should fade in.</param>
    /// <param name="spawnedItem">The item spawned.</param>
    /// <param name="pet">The pet to draw for.</param>
    public PetHudMessage(string message, float timeLeft, bool fadeIn, Item spawnedItem, Pet pet)
        : base(message, timeLeft, fadeIn)
    {
        this.spawnedItem = spawnedItem;
        this.messageWidth = ModEntry.StringUtils.MeasureWord(Game1.smallFont, this.message);
        pet.GetPetIcon(out string? assetName, out this.petRect);

        if (assetName is not null)
        {
            try
            {
                this.petTex = Game1.temporaryContent.Load<Texture2D>(assetName);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogOnce($"Could not load texture for {pet.Name} - {assetName}", LogLevel.Warn);
                ModEntry.ModMonitor.Log(ex.ToString());
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>Draws in the hudmessage. Copied and edited from game code.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override void draw(SpriteBatch b, int i, ref int heightUsed)
    {
        Rectangle tsarea = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();
        const int height = 112;
        Vector2 itemBoxPosition = new(tsarea.Left + 16, tsarea.Bottom - height - heightUsed - 64);
        heightUsed += height;
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
            texture: Game1.mouseCursors,
            position: itemBoxPosition,
            new Rectangle(293, 360, 26, 24),
            color: Color.White * this.transparency,
            rotation: 0f,
            origin: Vector2.Zero,
            scale: Game1.pixelZoom,
            effects: SpriteEffects.None,
            layerDepth: 0.99f);

        // draws the bit the message sits in.
        b.Draw(
            texture: Game1.mouseCursors,
            new Vector2(itemBoxPosition.X + 104f, itemBoxPosition.Y),
            new Rectangle(319, 360, 1, 24),
            color: Color.White * this.transparency,
            rotation: 0f,
            origin: Vector2.Zero,
            new Vector2(this.messageWidth, 4f),
            effects: SpriteEffects.None,
            layerDepth: 0.99f);

        // draw the right side of the box.
        b.Draw(
            texture: Game1.mouseCursors,
            new Vector2(itemBoxPosition.X + 104f + this.messageWidth, itemBoxPosition.Y),
            new Rectangle(323, 360, 6, 24),
            color: Color.White * this.transparency,
            rotation: 0f,
            origin: Vector2.Zero,
            scale: Game1.pixelZoom,
            effects: SpriteEffects.None,
            layerDepth: 0.99f);
        itemBoxPosition.X += 16f;
        itemBoxPosition.Y += 16f;

        // draw item.
        this.spawnedItem.drawInMenu(
            spriteBatch: b,
            location: itemBoxPosition,
            scaleSize: 1f,
            transparency: this.transparency,
            layerDepth: 1f,
            drawStackNumber: StackDrawType.Hide);

        // draw pet head.
        if (this.petTex is Texture2D tex)
        {
            b.Draw(
                texture: tex,
                position: itemBoxPosition + (new Vector2(8f, 8f) * 4f),
                this.petRect,
                color: Color.White * this.transparency,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: Game1.pixelZoom,
                effects: SpriteEffects.None,
                layerDepth: 0.99f);
        }

        itemBoxPosition.X += 83f;
        itemBoxPosition.Y += 18f;
        Utility.drawTextWithShadow(
            b,
            text: this.message,
            font: Game1.smallFont,
            position: itemBoxPosition,
            color: Game1.textColor * this.transparency,
            scale: 1f,
            layerDepth: 1f,
            horizontalShadowOffset: -1,
            verticalShadowOffset: -1,
            shadowIntensity: this.transparency);
    }
}
