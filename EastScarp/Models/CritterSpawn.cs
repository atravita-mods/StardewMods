namespace EastScarp.Models;

/// <summary>
/// Data to spawn a critter.
/// </summary>
public sealed class CritterSpawn : LocationArea
{
    /// <summary>
    /// When this trigger should apply.
    /// </summary>
    public SpawnTrigger Trigger { get; set; } = SpawnTrigger.OnEntry;

    public CritterType Critter { get; set; } = CritterType.Seagull;

    public float Chance { get; set; } = 1f;

    public float ChanceOnLand { get; set; } = 1f;

    public float ChanceOnWater { get; set; } = 1f;

    public RRange Clusters { get; set; } = new();

    public RRange CountPerCluster { get; set; } = new();
}
