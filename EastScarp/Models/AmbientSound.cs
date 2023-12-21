namespace EastScarp.Models;

/// <summary>
/// Represents an ambient sound to play.
/// </summary>
public sealed class AmbientSound
    : LocationArea
{
    /// <summary>
    /// Gets or sets the trigger to use.
    /// </summary>
    public SpawnTrigger Trigger { get; set; }

    /// <summary>
    /// Gets or sets the sound to play.
    /// </summary>
    public string Sound { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets if given, a list of pitches to use.
    /// </summary>
    public List<int>? Pitches { get; set; }
}
