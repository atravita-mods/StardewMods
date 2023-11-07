namespace EastScarp.Models;

/// <summary>
/// Represents an ambient sound to play.
/// </summary>
public sealed class AmbientSound: LocationArea
{
    /// <summary>
    /// The trigger to use.
    /// </summary>
    public SpawnTrigger Trigger { get; set; }

    /// <summary>
    /// The sound to play.
    /// </summary>
    public string Sound { get; set; } = string.Empty;

    /// <summary>
    /// If given, a list of pitches to use.
    /// </summary>
    public List<int>? Pitches { get; set; }
}
