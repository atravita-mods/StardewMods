namespace CameraPan.Framework;

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

    public float XRange
    {
        get => this.xRange;
        set => this.xRange = Math.Max(value, 1f);
    }

    public float YRange
    {
        get => this.yRange;
        set => this.yRange = Math.Max(value, 1f);
    }
}
