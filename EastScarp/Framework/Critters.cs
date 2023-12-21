namespace EastScarp.Framework;

using EastScarp.Models;

using Microsoft.Xna.Framework;

using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;

// TODO: bunnies.

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

            ModEntry.ModMonitor.VerboseLog($"Spawning {clusters} {spawn.Critter.ToStringFast()} at {location.NameOrUniqueName}.");

            Rectangle area = spawn.Area.ClampMap(location);

            for (int i = 0; i < clusters; i++)
            {
                Vector2 center = new (
                    Random.Shared.Next(area.Left, area.Right + 1),
                    Random.Shared.Next(area.Top, area.Bottom + 1));

                int num = spawn.CountPerCluster.Get();
                if (num < 1)
                {
                    break;
                }

                ModEntry.ModMonitor.VerboseLog($"    Cluster of {num} at {center}.");
                foreach (Vector2 tile in Utility.getPositionsInClusterAroundThisTile(center, num))
                {
                    // some critters should spawn on the map.
                    if (spawn.Critter > CritterType.Owl && !location.isTileOnMap(center))
                    {
                        continue;
                    }

                    if (spawn.Critter <= CritterType.Owl && Utility.isOnScreen(center.ToPoint(), 64, location))
                    {
                        // flying critters need to be off screen or else they appear to just pop in.
                        continue;
                    }

                    int tileX = (int)tile.X;
                    int tileY = (int)tile.Y;

                    bool waterTile = location.isWaterTile(tileX, tileY) && location.doesTileHaveProperty(tileX, tileY, "Passable", "Buildings") is null;
                    if (!waterTile && location.CanItemBePlacedHere(tile, false, CollisionMask.All, CollisionMask.Flooring))
                    {
                        continue;
                    }

                    float chance = waterTile ? spawn.ChanceOnWater : spawn.ChanceOnLand;
                    if (Random.Shared.NextBool(chance))
                    {
                        Critter? critter = spawn.Critter switch
                        {
                            CritterType.BrownBird =>
                                new Birdie((int)tile.X, (int)tile.Y, Birdie.brownBird),
                            CritterType.BlueBird =>
                                new Birdie((int)tile.X, (int)tile.Y, Birdie.blueBird),
                            CritterType.SpecialBlueBird =>
                                new Birdie((int)tile.X, (int)tile.Y, 125),
                            CritterType.SpecialRedBird =>
                                new Birdie((int)tile.X, (int)tile.Y, 135),
                            CritterType.Butterfly =>
                                new Butterfly(location, tile),
                            CritterType.IslandButterfly =>
                                new Butterfly(location, tile, true),
                            CritterType.CalderaMonkey =>
                                new CalderaMonkey(tile * 64f),
                            CritterType.Cloud =>
                                new Cloud(tile),
                            CritterType.Crab =>
                                new CrabCritter(tile * 64f),
                            CritterType.Crow =>
                                new Crow((int)tile.X, (int)tile.Y),
                            CritterType.Firefly =>
                                new Firefly(tile),
                            CritterType.Frog =>
                                new Frog(tile, waterLeaper: waterTile, forceFlip: Game1.random.NextDouble() < 0.5),
                            CritterType.OverheadParrot =>
                                new OverheadParrot(tile * 64f),
                            CritterType.Owl =>
                                new Owl(tile * 64f),
                            CritterType.Rabbit =>
                                new Rabbit(location, tile, flip: Game1.random.NextDouble() < 0.5),
                            CritterType.Seagull =>
                                new Seagull((tile * 64f) + new Vector2(32f, 32f), startingState: waterTile ? Seagull.swimming : Seagull.stopped),
                            CritterType.Squirrel =>
                                new Squirrel(tile, flip: Game1.random.NextDouble() < 0.5),
                            _ => null,
                        };
                        if (critter is not null)
                        {
                            location.instantiateCrittersList();
                            location.critters.Add(critter);
                        }
                    }
                }
            }
        }
    }
}
