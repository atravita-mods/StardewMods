namespace EastScarp.Models;

/// <summary>
/// The additional per-location data.
/// </summary>
public sealed class LocationDataModel
{
    /// <summary>
    /// Gets a list of ambient sounds to play.
    /// </summary>
    public List<AmbientSound> Sounds { get; set; } = new();

    /// <summary>
    /// Gets a list of sea monsters to spawn.
    /// </summary>
    public List<SeaMonsterSpawn> SeaMonsterSpawn { get; set; } = new();

    /// <summary>
    /// Gets a list of water colors to set on warp.
    /// </summary>
    public List<WaterColor> WaterColor { get; set; } = new();

    /// <summary>
    /// Gets a list of critters to spawn.
    /// </summary>
    public List<CritterSpawn> Critters { get; set; } = new();
}