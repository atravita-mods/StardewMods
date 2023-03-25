using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace CameraPan;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    internal const int CAMERA_ID = 106;

    private static readonly PerScreen<Vector2> offset = new (() => Vector2.Zero);

    //internal static Vector2 Offset

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.UpdateTicked += this.OnTicked;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button == SButton.Escape)
        {
            offset.Value = Vector2.Zero;
        }
    }

    private void OnTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsPlayerFree)
        {
            return;
        }
        Vector2 pos = this.Helper.Input.GetCursorPosition().ScreenPixels;
        Vector2 adjustment = Vector2.Zero;
        if (pos.X < (Game1.viewport.Width / 8))
        {
            adjustment.X = -8;
        }
        else if (pos.X > Game1.viewport.Width - (Game1.viewport.Width / 8))
        {
            adjustment.X = 8;
        }

        if (pos.Y < (Game1.viewport.Height / 8))
        {
            adjustment.Y = -8;
        }
        else if (pos.Y > Game1.viewport.Height - (Game1.viewport.Height / 8))
        {
            adjustment.Y = 8;
        }

        offset.Value += adjustment;
        Game1.moveViewportTo(Game1.player.Position + offset.Value, 8f);
    }
}