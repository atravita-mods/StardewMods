namespace EastScarp.Framework;

using EastScarp.Models;

using Microsoft.Xna.Framework;

using StardewValley.Extensions;

/// <summary>
/// Manages critters.
/// </summary>
internal static class Critters
{
    /// <summary>
    /// Spawns critters for this mod.
    /// </summary>
    /// <param name="spawns">Critters to spawn.</param>
    /// <param name="trigger">The trigger condition.</param>
    /// <param name="location">The location to spawn critters.</param>
    /// <param name="farmer">The farmer instance.</param>
    internal static void SpawnCritter(List<CritterSpawn> spawns, SpawnTrigger trigger, GameLocation location, Farmer farmer)
    {
        foreach (CritterSpawn spawn in spawns)
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

            int clusters = spawn.Clusters.Get();
            if (clusters < 1)
            {
                continue;
            }

            Rectangle area = spawn.Area.ClampMap(location);
            Vector2 center = new (
                Random.Shared.Next(area.Left, area.Right + 1),
                Random.Shared.Next(area.Top, area.Bottom + 1));

            // some critters should spawn on the map.
            if (spawn.Critter > CritterType.Owl && !location.isTileOnMap(center))
            {
                continue;
            }

            ModEntry.ModMonitor.VerboseLog($"Spawning {clusters} {spawn.Critter.ToStringFast()} at {location.NameOrUniqueName} ({center}).");

        }
    }
}
