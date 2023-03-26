using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

using CameraPan.Framework;

using AtraUtils = AtraShared.Utils.Utils;

namespace CameraPan;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    internal const int CAMERA_ID = 106;

    internal static ModConfig Config { get; private set; } = null!;

    private static readonly PerScreen<Vector2> offset = new (() => Vector2.Zero);

    internal static Vector2 Offset
    {
        get => offset.Value;
        set => offset.Value = value;
    }

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.Events.GameLoop.UpdateTicked += this.OnTicked;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;

        helper.Events.Player.Warped += this.OnWarped;
        helper.Events.Display.MenuChanged += this.OnMenuChanged;
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.OldMenu is not null && e.NewMenu is null)
        {
            offset.Value = Vector2.Zero;
            Game1.viewportTarget = new Vector2(-2.14748365E+09f, -2.14748365E+09f);
        }
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (e.IsLocalPlayer)
        {
            offset.Value = Vector2.Zero;
            Game1.viewportTarget = new Vector2(-2.14748365E+09f, -2.14748365E+09f);
        }
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
            adjustment.X = -Config.Speed;
        }
        else if (pos.X > Game1.viewport.Width - (Game1.viewport.Width / 8))
        {
            adjustment.X = Config.Speed;
        }

        if (pos.Y < (Game1.viewport.Height / 8))
        {
            adjustment.Y = -Config.Speed;
        }
        else if (pos.Y > Game1.viewport.Height - (Game1.viewport.Height / 8))
        {
            adjustment.Y = Config.Speed;
        }

        Vector2 temp = offset.Value + adjustment;

        temp.X = Math.Clamp(temp.X, -Config.XRange, Config.XRange);
        temp.Y = Math.Clamp(temp.Y, -Config.YRange, Config.YRange);

        offset.Value = temp;
        Game1.moveViewportTo(Game1.player.Position + offset.Value, Config.Speed);
    }
}