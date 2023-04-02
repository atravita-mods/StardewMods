using AtraShared.Integrations.GMCMAttributes;

using NetEscapades.EnumGenerators;

using StardewModdingAPI.Utilities;

namespace CameraPan.Framework;

/// <summary>
/// The config class for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Fields kept near accessors.")]
public sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating how the panning should be toggled.
    /// </summary>
    public ToggleBehavior ToggleBehavior { get; set; } = ToggleBehavior.Toggle;

    private int speed = 8;

    /// <summary>
    /// Gets or sets the speed to move the camera at.
    /// </summary>
    [GMCMRange(1, 24)]
    public int Speed
    {
        get => this.speed;
        set => this.speed = Math.Clamp(value, 1, 24);
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

    /// <summary>
    /// Gets or sets a value indicating which button should be used to toggle the panning.
    /// </summary>
    public KeybindList ToggleButton { get; set; } = new(new(SButton.O), new(SButton.RightTrigger));

    /// <summary>
    /// Gets or sets the button used to reset the camera behind the player.
    /// </summary>
    public KeybindList ResetButton { get; set; } = new(new(SButton.R), new(SButton.RightStick));

    /// <summary>
    /// Gets or sets the button used to set the camera upwards.
    /// </summary>
    public KeybindList UpButton { get; set; } = new(new(SButton.Up), new(SButton.RightThumbstickUp));

    /// <summary>
    /// Gets or sets the button used to set the camera downwards.
    /// </summary>
    public KeybindList DownButton { get; set; } = new(new(SButton.Down), new(SButton.RightThumbstickDown));

    /// <summary>
    /// Gets or sets the button used to set the camera leftwards.
    /// </summary>
    public KeybindList LeftButton { get; set; } = new(new(SButton.Left), new(SButton.RightThumbstickLeft));

    /// <summary>
    /// Gets or sets the button used to set the camera rightwards.
    /// </summary>
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
[EnumExtensions]
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

/// <summary>
/// Indicates how the camera panning should be toggled.
/// </summary>
public enum ToggleBehavior
{
    /// <summary>
    /// Camera panning should never be allowed.
    /// </summary>
    Never,

    /// <summary>
    /// A hotkey controls camera panning.
    /// </summary>
    Toggle,

    /// <summary>
    /// Holding the camera object allows panning.
    /// </summary>
    Camera,

    /// <summary>
    /// Panning is always enabled.
    /// </summary>
    Always,
}