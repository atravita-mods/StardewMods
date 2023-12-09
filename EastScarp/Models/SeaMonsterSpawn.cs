namespace EastScarp.Models;

/// <summary>
/// A trigger to spawn the sea monster.
/// </summary>
public sealed class SeaMonsterSpawn : LocationArea
{
    public SpawnTrigger Trigger { get; set; } = SpawnTrigger.OnEntry;

    public float Chance { get; set; } = 1f;
}
