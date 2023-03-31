using StardewModdingAPI.Utilities;

namespace CameraPan.Framework;

/// <summary>
/// The config class for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Fields kept near accessors.")]
public sealed class ModConfig
{
    private int speed = 8;

    /// <summary>
    /// Gets or sets the speed to move the camera at.
    /// </summary>
    public int Speed
    {
        get => this.speed;
        set => this.speed = Math.Clamp(value, 1, 20);
    }

    private int xRange = 1000;

    private int yRange = 1000;

    /// <summary>
    /// Gets or sets the maximum distance the focal point can be from the player, on the x axis.
    /// </summary>
    public int XRange
    {
        get => this.xRange;
        set => this.xRange = Math.Max(value, 1);
    }

    /// <summary>
    /// Gets or sets the maximum distance the focal point can be from the player, on the y axis.
    /// </summary>
    public int YRange
    {
        get => this.yRange;
        set => this.yRange = Math.Max(value, 1);
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not players should be kept on screen.
    /// </summary>
    public bool KeepPlayerOnScreen { get; set; } = true;

    public KeybindList ResetButton { get; set; } = new(new(SButton.R), new(SButton.RightStick));

    public KeybindList UpButton { get; set; } = new(new(SButton.Up), new(SButton.RightThumbstickUp));

    public KeybindList DownButton { get; set; } = new(new(SButton.Down), new(SButton.RightThumbstickDown));

    public KeybindList LeftButton { get; set; } = new(new(SButton.Left), new(SButton.RightThumbstickLeft));

    public KeybindList RightButton { get; set; } = new(new(SButton.Right), new(SButton.RightThumbstickRight));

    #region internal

    private int xRangeActual = 1000;
    private int yRangeActual = 1000;

    internal int XRangeInternal => this.xRangeActual;

    internal int YRangeInternal => this.yRangeActual;

    /// <summary>
    /// Recalculates the actual bounds that should be used for the viewport, taking into account the window size.
    /// </summary>
    internal void RecalculateBounds()
    {
        if (this.KeepPlayerOnScreen)
        {
            this.xRangeActual = Math.Max(0, Math.Min(this.XRange, (Game1.viewport.Width / 2) - 64));
            this.yRangeActual = Math.Max(0, Math.Min(this.YRange, (Game1.viewport.Height / 2) - 128));
        }
        else
        {
            this.xRangeActual = this.XRange;
            this.yRangeActual = this.YRange;
        }
    }

    #endregion
}

/// <summary>
/// Controls how the camera should behave.
/// </summary>
[Flags]
public enum CameraBehavior
{
    /// <summary>
    /// Use the vanilla behavior.
    /// </summary>
    Vanila = 0,

    /// <summary>
    /// Always keep the player in the center.
    /// </summary>
    Locked = 0b1,

    /// <summary>
    /// Apply the offset, if relevant.
    /// </summary>
    Offset = 0b10,

    /// <summary>
    /// Always keep the offset position in the center.
    /// </summary>
    Both = Locked | Offset,
}