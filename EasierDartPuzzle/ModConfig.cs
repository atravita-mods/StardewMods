namespace EasierDartPuzzle;

#pragma warning disable SA1201 // Elements should appear in the correct order. Backing fields kept near accessors.
/// <summary>
/// The config class for this mod.
/// </summary>
internal sealed class ModConfig
{
    private int mpPirateArrivalTime = 1600;

    /// <summary>
    /// Gets or sets when the pirates should show up in multiplayer.
    /// </summary>
    public int MPPirateArrivalTime
    {
        get => this.mpPirateArrivalTime;
        set => this.mpPirateArrivalTime = Math.Clamp(value, 600, 2000);
    }

    private int minDartCount = 10;

    public int MinDartCount
    {
        get => this.minDartCount;
        set => this.minDartCount = Math.Clamp(value, 8, 30);
    }

    private int maxDartCount = 20;

    public int MaxDartCount
    {
        get => this.maxDartCount;
        set => this.minDartCount = Math.Clamp(value, 8, 30);
    }

    public bool ShowDartMarker { get; set; } = false;

    private float jitterMultiplier = 1f;

    public float JitterMultiplier
    {
        get => this.jitterMultiplier;
        set => this.jitterMultiplier = Math.Clamp(value, 0.05f, 20f);
    }
}
#pragma warning restore SA1201 // Elements should appear in the correct order