namespace NPCArrows.Framework.NPCs;

using AtraShared.ConstantsAndEnums;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Draws in an arrow pointing at an off-screen.
/// </summary>
internal static class NPCHelpers
{
    /// <summary>
    /// Draws in an arrow pointing at an NPC.
    /// </summary>
    /// <param name="character">Character to draw arrow pointing at.</param>
    /// <param name="spriteBatch">Sprite batch to use.</param>
    internal static void DrawArrow(this NPC character, SpriteBatch spriteBatch)
    {
        Vector2 pos = character.Position + new Vector2(32f, 64f);

        Vector2 arrowPos = Game1.GlobalToLocal(Game1.viewport, pos);
        Direction direction = Direction.None;

        if (arrowPos.X <= 0)
        {
            direction |= Direction.Left;
            arrowPos.X = 8f;
        }
        else if (arrowPos.X >= Game1.viewport.Width)
        {
            direction |= Direction.Right;
            arrowPos.X = Game1.viewport.Width - 8f;
        }

        if (arrowPos.Y <= 0)
        {
            direction |= Direction.Up;
            arrowPos.Y = 8f;
        }
        else if (arrowPos.Y >= Game1.viewport.Height)
        {
            direction |= Direction.Down;
            arrowPos.Y = Game1.viewport.Height - 8f;
        }

        if (direction == Direction.None)
        {
            return;
        }

        arrowPos = Utility.snapToInt(Utility.ModifyCoordinatesForUIScale(arrowPos));

        spriteBatch.Draw(
            texture: AssetManager.ArrowTexture,
            position: arrowPos,
            sourceRectangle: null,
            color: Color.Coral,
            rotation: direction.GetRotationFacing(),
            origin: new Vector2(2f, 2f),
            scale: Game1.pixelZoom,
            effects: SpriteEffects.None,
            layerDepth: 1f);

        Rectangle mug = character.getMugShotSourceRect();
        mug.Height = Math.Min(24, mug.Height);
        spriteBatch.Draw(
            texture: character.Sprite.Texture,
            position: arrowPos - (direction.GetVectorFacing() * 48f) - new Vector2(32f, 48f),
            sourceRectangle: mug,
            color: Color.White,
            rotation: 0f,
            origin: Vector2.Zero,
            layerDepth: 1f,
            effects: SpriteEffects.None,
            scale: Game1.pixelZoom);
    }
}