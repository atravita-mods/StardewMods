namespace CameraPan.Framework;

/// <summary>
/// The config class for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Fields kept near accessors.")]
public sealed class ModConfig
{
    private float speed = 8f;

    /// <summary>
    /// Gets or sets the speed to move the camera at.
    /// </summary>
    public float Speed
    {
        get => this.speed;
        set => this.speed = Math.Clamp(value, 1f, 20f);
    }

    private float xRange = 1000f;

    private float yRange = 1000f;

    /// <summary>
    /// Gets or sets the maximum distance the focal point can be from the player, on the x axis.
    /// </summary>
    public float XRange
    {
        get => this.xRange;
        set => this.xRange = Math.Max(value, 1f);
    }

    /// <summary>
    /// Gets or sets the maximum distance the focal point can be from the player, on the y axis.
    /// </summary>
    public float YRange
    {
        get => this.yRange;
        set => this.yRange = Math.Max(value, 1f);
    }
}
