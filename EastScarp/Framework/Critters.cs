namespace EastScarp.Framework;

using EastScarp.Models;

using StardewValley.Extensions;

internal static class Critters
{
    internal static void SpawnCritter(List<CritterSpawn> spawns, SpawnTrigger trigger, GameLocation location, Farmer farmer)
    {
        foreach (var spawn in spawns)
        {
            if (!spawn.Trigger.HasFlag(trigger))
            {
                continue;
            }

            if (!spawn.CheckCondition(location, farmer))
            {
                continue;
            }

            if (!Random.Shared.NextBool(spawn.Chance))
            {
                continue;
            }



        }
    }
}
