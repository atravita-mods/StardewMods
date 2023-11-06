namespace EastScarp.Models;

/// <summary>
/// Data to spawn a critter.
/// </summary>
public sealed class CritterSpawn : LocationArea
{
    public SpawnTrigger Trigger { get; set; } = SpawnTrigger.OnEntry;

    public CritterType Critter { get; set; }

    public float Chance { get; set; }

    public float ChanceOnLand { get; set; }

    public float ChanceOnWater { get; set; }

    public RRange Clusters { get; set; }

    public RRange CountPerCluster { get; set; }
}
