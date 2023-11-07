namespace EastScarp.Framework;

using EastScarp.Models;

using Microsoft.Xna.Framework;

using StardewValley.Extensions;
internal static class SeaMonsterSpawner
{
    internal static void SpawnMonster(List<SeaMonsterSpawn> spawns, SpawnTrigger trigger, GameLocation location, Farmer farmer)
    {
        var maxHeight = location.Map?.GetLayer("Back")?.LayerHeight ?? 0;
        if (maxHeight < 1)
        {
            ModEntry.ModMonitor.LogOnce($"Could not find 'back' layer for {location.NameOrUniqueName ?? "Unknown Location"}", LogLevel.Warn);
            return;
        }

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

            var area = spawn.Area.ClampMap(location);

            if (area == Rectangle.Empty)
            {
                continue;
            }

            int x = Random.Shared.Next(area.X, area.Right + 1);
            int y = Random.Shared.Next(area.Y, Math.Min(area.Bottom + 1, maxHeight));

            // confirm sea monster can swim off screen.I can honest

            for (int dy = maxHeight - 1; dy >= y; dy--)
            {
                if (!location.isWaterTile(x, dy) || !location.isWaterTile(x - 1, y) || !location.isWaterTile(x + 1, y))
                {
                    continue;
                }
            }

            // spawn monster
            ModEntry.ModMonitor.Log($"Spawning sea monster at {location.Name} ({x}, {y}).");
            location!.temporarySprites.Add(new SeaMonsterTemporarySprite(
                animationInterval: 250f,
                animationLength: 4,
                numberOfLoops: Random.Shared.Next(7),
                position: new Vector2(x, y) * Game1.tileSize));
            return;
        }
    }
}
