namespace EastScarp.Framework;

using EastScarp.Models;

using StardewValley.Extensions;
internal static class SeaMonsterSpawner
{
    internal static void SpawnMonster(List<SeaMonsterSpawn> spawns, SpawnTrigger trigger, GameLocation location, Farmer farmer)
    {
        foreach (var spawn in spawns)
        {
            if (!spawn.Trigger.HasFlag(trigger))
            {
                return;
            }

            if (!spawn.CheckCondition(location, farmer))
            {
                return;
            }

            if (Random.Shared.NextBool(spawn.Chance))
            {
                return;
            }
        }
    }
}
