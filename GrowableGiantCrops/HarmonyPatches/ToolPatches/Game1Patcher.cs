using GrowableGiantCrops.Framework;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GrowableGiantCrops.HarmonyPatches.ToolPatches;

[HarmonyPatch(typeof(Game1))]
internal static class Game1Patcher
{
    /// <summary>
    /// Draws the shovel. Highly derived from Game1.drawTool
    /// </summary>
    /// <param name="f">Farmer.</param>
    /// <param name="currentToolIndex">the current tool index.</param>
    /// <returns></returns>
    [HarmonyPatch(nameof(Game1.drawTool), new[] { typeof(Farmer), typeof(int) })]
    private static bool Prefix(Farmer f, int currentToolIndex)
    {
        if (f.CurrentTool is not ShovelTool shovel)
        {
            return true;
        }

        ModEntry.ModMonitor.LogOnce(f.Sprite.CurrentFrame.ToString(), LogLevel.Alert);

        Rectangle sourceRectangleForTool = new(currentToolIndex * 16, 0, 16, 32);
        Vector2 fPosition = f.getLocalPosition(Game1.viewport) + f.jitter + f.armOffset;
        float tool_draw_layer_offset = 0f;
        if (f.FacingDirection == 0)
        {
            tool_draw_layer_offset = -0.002f;
        }

        if (Game1.pickingTool)
        {
            int yLocation = (int)fPosition.Y - 128;
            Game1.spriteBatch.Draw(
                texture: AssetManager.ToolTexture,
                new Vector2(fPosition.X, yLocation),
                sourceRectangle: sourceRectangleForTool,
                color: Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 1f,
                effects: SpriteEffects.None,
                layerDepth: Math.Max(0f, tool_draw_layer_offset + ((f.getStandingY() + 32) / 10000f)));
            return false;
        }

        if (f.CurrentTool is not null)
        {
            f.CurrentTool.draw(Game1.spriteBatch);
        }

        Vector2 position;
        float rotation = 0f;
        Vector2 origin = new Vector2(0f, 16f);

        switch (f.FacingDirection)
        {
            case 0:
                switch (f.Sprite.currentAnimationIndex)
                {
                    case 0:
                        position = Utility.snapToInt(new Vector2(fPosition.X, fPosition.Y - 128f - 8f + (float)Math.Min(8, f.toolPower * 4)));
                        break;
                    case 1:
                        position = Utility.snapToInt(new Vector2(fPosition.X + 4f, fPosition.Y - 128f + 40f));
                        break;
                    case 2:
                        position = Utility.snapToInt(new Vector2(fPosition.X, fPosition.Y - 64f));
                        break;
                    default:
                        return false;
                }
                break;
            case 1:
                switch (f.Sprite.currentAnimationIndex)
                {
                    case 0:
                        position = Utility.snapToInt(new Vector2(fPosition.X - 32f - 4f - Math.Min(8, f.toolPower * 4), fPosition.Y - 128f + 24f + Math.Min(8, f.toolPower * 4)));
                        rotation = (-MathF.PI / 12f) - (Math.Min(f.toolPower, 2) * (MathF.PI / 64f));
                        break;
                    case 1:
                        position = Utility.snapToInt(new Vector2(fPosition.X + 32f - 24f, fPosition.Y - 124f + 64f));
                        rotation = -MathF.PI / 12f;
                        origin = new Vector2(0f, 32f);
                        break;
                    case 2:
                        position = Utility.snapToInt(new Vector2(fPosition.X + 32f - 4f, fPosition.Y - 132f + 64f));
                        rotation = MathF.PI / 4f;
                        origin = new Vector2(0f, 32f);
                        break;
                    case 3:
                        position = Utility.snapToInt(new Vector2(fPosition.X + 32f + 28f, fPosition.Y - 64f));
                        rotation = MathF.PI * 7f / 12f;
                        origin = new Vector2(0f, 32f);
                        break;
                    case 4:
                        position = Utility.snapToInt(new Vector2(fPosition.X + 32f + 28f, fPosition.Y - 64f + 4f));
                        rotation = MathF.PI * 7f / 12f;
                        origin = new Vector2(0f, 32f);
                        break;
                    default:
                        return false;
                }
                break;
            case 2:
                switch (f.Sprite.currentAnimationIndex)
                {
                    case 0:
                        position = Utility.snapToInt(new Vector2(fPosition.X - 20f, fPosition.Y - 128f + 12f + Math.Min(8, f.toolPower * 4)));
                        break;
                    case 1:
                        position = Utility.snapToInt(new Vector2(fPosition.X - 12f, fPosition.Y - 128f + 32f));
                        rotation = -MathF.PI / 24f;
                        break;
                    case 2:
                        position = Utility.snapToInt(new Vector2(fPosition.X, fPosition.Y - 64f));
                        break;
                    case 3:
                        position = Utility.snapToInt(new Vector2(fPosition.X, fPosition.Y - 20f));
                        break;
                    case 4:
                        position = Utility.snapToInt(new Vector2(fPosition.X, fPosition.Y - 16f));
                        break;
                    default:
                        return true;
                }
                break;
            case 3:
                switch (f.Sprite.currentAnimationIndex)
                {
                    case 0:
                        position = Utility.snapToInt(new Vector2(fPosition.X + 32f + 8f + Math.Min(8, f.toolPower * 4), fPosition.Y - 128f + 8f + Math.Min(8, f.toolPower * 4)));
                        rotation = MathF.PI / 12f + Math.Min(f.toolPower, 2) * (MathF.PI / 64f);
                        break;
                    case 1:
                        position = Utility.snapToInt(new Vector2(fPosition.X - 16f, fPosition.Y - 128f + 16f));
                        rotation = MathF.PI / 12f;
                        break;
                    case 2:
                        position = Utility.snapToInt(new Vector2(fPosition.X - 64f + 4f, fPosition.Y - 128f + 60f));
                        rotation = -MathF.PI / 4f;
                        break;
                    case 3:
                        position = Utility.snapToInt(new Vector2(fPosition.X - 64f + 24f, fPosition.Y + 12f));
                        rotation = -MathF.PI * 7f / 12f;
                        break;
                    case 4:
                        position = Utility.snapToInt(new Vector2(fPosition.X - 64f + 24f, fPosition.Y + 24f));
                        rotation = -MathF.PI * 7f / 12f;
                        break;
                    default:
                        return false;
                }
                break;
            default:
                return false;
        }

        Game1.spriteBatch.Draw(
            texture: AssetManager.ToolTexture,
            position,
            sourceRectangle: sourceRectangleForTool,
            color: Color.White,
            rotation,
            origin,
            scale: 4f,
            effects: SpriteEffects.None,
            layerDepth: Math.Max(0f, tool_draw_layer_offset + f.GetBoundingBox().Bottom / 10000f));

        return false;
    }
}
