namespace NerfCavePuzzle;

/// <summary>
/// The config class for this mod.
/// </summary>
internal class ModConfig
{
    private float speedModifier = 1f;
    private float flashScale = 1f;
    private int maxNotes = 7;

    /// <summary>
    /// Gets or sets the speed modifer.
    /// </summary>
    public float SpeedModifer
    {
        get => this.speedModifier;
        set => this.speedModifier = Math.Clamp(value, 0.1f, 10f);
    }

    /// <summary>
    /// Gets or sets the scaling factor for the flash speed.
    /// </summary>
    public float FlashScale
    {
        get => this.flashScale;
        set => this.flashScale = Math.Clamp(value, 0.1f, 10f);
    }

    /// <summary>
    /// Gets or sets a value that caps the maximum amount of notes.
    /// </summary>
    public int MaxNotes
    {
        get => this.maxNotes;
        set => this.maxNotes = Math.Clamp(value, 4, 7);
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not to pause between rounds.
    /// </summary>
    public bool PauseBetweenRounds { get; set; } = true;
}