using AtraBase.Toolkit.Reflection;
using AtraCore.Framework.ReflectionManager;
using AtraCore.Utilities;
using Microsoft.Xna.Framework;
using StardewValley.BellsAndWhistles;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace CritterRings.Framework;

/// <summary>
/// A utility class for this mod.
/// </summary>
internal static class CRUtils
{
    #region delegates

    private static readonly Lazy<Action<Rabbit, int>> CharacterTimerSetter = new(() =>
        typeof(Rabbit).GetCachedField("characterCheckTimer", ReflectionCache.FlagTypes.InstanceFlags)
        .GetInstanceFieldSetter<Rabbit, int>()
    );

    private static readonly Lazy<Action<Frog, int>> FrogTimerSetter = new(() =>
        typeof(Frog).GetCachedField("beforeFadeTimer", ReflectionCache.FlagTypes.InstanceFlags)
        .GetInstanceFieldSetter<Frog, int>()
);

    #endregion

    /// <summary>
    /// Plays the sound associated with charging up.
    /// </summary>
    /// <param name="charge">The charge amount.</param>
    internal static void PlayChargeCue(int charge)
    {
        if (ModEntry.Config.PlayAudioEffects && Game1.soundBank is not null)
        {
            try
            {
                ICue cue = Game1.soundBank.GetCue("toolCharge");
                cue.SetVariable("Pitch", (Game1.random.Next(12, 16) + charge ) * 100);
                cue.Play();
            }
            catch (Exception ex)
            {
                ModEntry.Config.PlayAudioEffects = false;
                ModEntry.ModMonitor.Log($"Failed while trying to play charge-up cue!\n\n{ex}", LogLevel.Error);
            }
        }
    }

    internal static void PlayMeep()
    {
        if (ModEntry.Config.PlayAudioEffects && Game1.soundBank is not null)
        {
            try
            {
                Game1.playSound("dustMeep");
            }
            catch (Exception ex)
            {
                ModEntry.Config.PlayAudioEffects = false;
                ModEntry.ModMonitor.Log($"Failed while trying to play hopping noise.\n\n{ex}", LogLevel.Error);
            }
        }
    }

    /// <summary>
    /// Checks to make sure it's safe to spawn butterflies.
    /// </summary>
    /// <param name="loc">Game location to check.</param>
    /// <returns>True if we should spawn butterflies, false otherwise.</returns>
    internal static bool ShouldSpawnButterflies([NotNullWhen(true)] this GameLocation? loc)
        => loc is not null && !Game1.isDarkOut()
            && (ModEntry.Config.ButterfliesSpawnInRain || !loc.IsOutdoors || !Game1.IsRainingHere(loc));

    /// <summary>
    /// Checks to make sure it's safe to spawn frogs.
    /// </summary>
    /// <param name="loc">GameLocation to check.</param>
    /// <returns>True if this is a good place to spawn frogs.</returns>
    internal static bool ShouldSpawnFrogs([NotNullWhen(true)] this GameLocation? loc)
    {
        if (loc is null)
        {
            return false;
        }

        if (ModEntry.Config.FrogsSpawnOnlyInRain && !Game1.IsRainingHere(loc) && loc.IsOutdoors)
        {
            return false;
        }

        if (!ModEntry.Config.FrogsSpawnInHeat && loc is Desert or VolcanoDungeon or Caldera)
        {
            return false;
        }

        MineShaft? shaft = loc as MineShaft;

        if (!ModEntry.Config.FrogsSpawnInHeat && shaft?.getMineArea() == 80)
        {
            return false;
        }

        if (!ModEntry.Config.FrogsSpawnInCold && ((Game1.GetSeasonForLocation(loc) == "winter" && loc is not Desert or IslandLocation) || shaft?.getMineArea() == 40))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Spawns fireflies around the player.
    /// </summary>
    /// <param name="critters">Critters list to add to.</param>
    /// <param name="count">Number of fireflies to spawn.</param>
    internal static void SpawnFirefly(List<Critter>? critters, int count)
    {
        if (critters is not null && count > 0)
        {
            count *= ModEntry.Config.CritterSpawnMultiplier;
            for (int i = 0; i < count; i++)
            {
                critters.Add(new Firefly(Game1.player.getTileLocation()));
            }
        }
    }

    /// <summary>
    /// Spawns butterflies around the player.
    /// </summary>
    /// <param name="critters">Critters list to add to.</param>
    /// <param name="count">Number of butterflies to spawn.</param>
    internal static void SpawnButterfly(List<Critter>? critters, int count)
    {
        if (critters is not null && count > 0)
        {
            count *= ModEntry.Config.CritterSpawnMultiplier;
            for (int i = 0; i < count; i++)
            {
                critters.Add(new Butterfly(Game1.player.getTileLocation(), Game1.random.Next(2) == 0).setStayInbounds(true));
            }
        }
    }

    /// <summary>
    /// Spawns frogs around the player.
    /// </summary>
    /// <param name="critters">Critters list to add to.</param>
    /// <param name="count">Number of frogs to spawn.</param>
    internal static void SpawnFrogs(List<Critter> critters, int count)
    {
        if (critters is not null && count > 0)
        {
            count *= ModEntry.Config.CritterSpawnMultiplier;
            for (int i = 0; i < count; i++)
            {
                Frog frog = new(Game1.player.getTileLocation());
                FrogTimerSetter.Value(frog, Game1.random.Next(2000, 5000));
                critters.Add(frog);
            }
        }
    }

    /// <summary>
    /// Add bunnies to a location.
    /// </summary>
    /// <param name="critters">The critter list.</param>
    /// <param name="count">The number of bunnies to spawn.</param>
    internal static void AddBunnies(List<Critter> critters, int count, List<Bush>? bushes)
    {
        if (critters is not null && count > 0)
        {
            int delay = 0;
            foreach ((Vector2 position, bool flipped) in FindBunnySpawnTile(
                loc: Game1.currentLocation,
                bushes: bushes,
                playerTile: Game1.player.getTileLocation(),
                count: count * 2))
            {
                GameLocation location = Game1.currentLocation;
                DelayedAction.functionAfterDelay(
                func: () =>
                {
                    if (location == Game1.currentLocation)
                    {
                        SpawnRabbit(critters, position, location, flipped);
                    }
                },
                timer: delay += Game1.random.Next(250, 750));
            }
        }
    }

    private static IEnumerable<(Vector2, bool)> FindBunnySpawnTile(GameLocation loc, List<Bush>? bushes, Vector2 playerTile, int count)
    {
        if (count <= 0 || bushes?.Count is null or 0)
        {
            yield break;
        }

        Utility.Shuffle(Game1.random, bushes);

        count *= ModEntry.Config.CritterSpawnMultiplier;
        foreach (Bush bush in bushes)
        {
            if (count <= 0)
            {
                yield break;
            }

            if (Vector2.DistanceSquared(bush.tilePosition.Value, playerTile) <= 225)
            {
                if (bush.size.Value == Bush.walnutBush && bush.tileSheetOffset.Value == 1)
                {
                    // this is a walnut bush. Turns out bunnies can collect those.
                    continue;
                }

                bool flipped = Game1.random.Next(2) == 0;
                Vector2 startTile = bush.tilePosition.Value;
                startTile.X += flipped ? 2 : -2;
                int distance = Game1.random.Next(5, 12);

                for (int i = distance; i > 0; i--)
                {
                    Vector2 tile = startTile;
                    startTile.X += flipped ? 1 : -1;
                    if (!bush.getBoundingBox().Intersects(new Rectangle((int)startTile.X * 64, (int)startTile.Y * 64, 64, 64))
                        && (!loc.isTileLocationTotallyClearAndPlaceable(startTile) || loc.isWaterTile((int)startTile.X, (int)startTile.Y)))
                    {
                        yield return (tile, flipped);
                        count--;
                        goto Continue;
                    }
                }
                yield return (startTile, flipped);
                count--;
Continue:;
            }
        }
    }

    private static void SpawnRabbit(List<Critter>? critters, Vector2 tile, GameLocation loc, bool flipped)
    {
        if (critters is not null)
        {
            Rabbit rabbit = new(tile, flipped);
            // make the rabbit hang around for a little longer.
            // so it doesn't immediately exist stage left.
            CharacterTimerSetter.Value(rabbit, Game1.random.Next(750, 1500));
            critters.Add(rabbit);

            // little TAS to hide the pop in.
            TemporaryAnimatedSprite? tas = new(
                textureName: Game1.mouseCursorsName,
                sourceRect: new Rectangle(464, 1792, 16, 16),
                animationInterval: 120f,
                animationLength: 5,
                numberOfLoops: 0,
                position: (tile - Vector2.One) * 64f,
                flicker: false,
                flipped: Game1.random.NextDouble() < 0.5,
                layerDepth: 1f,
                alphaFade: 0.01f,
                color: Color.White,
                scale: Game1.pixelZoom,
                scaleChange: 0.01f,
                rotation: 0f,
                rotationChange: 0f)
            {
                light = true,
            };
            MultiplayerHelpers.GetMultiplayer().broadcastSprites(loc, tas);
        }
    }
}