using AtraCore.Utilities;
using AtraCore.Framework.ReflectionManager;

using Microsoft.Xna.Framework;

using StardewValley.BellsAndWhistles;
using StardewValley.TerrainFeatures;
using AtraBase.Toolkit.Reflection;

namespace CritterRings.Framework;

/// <summary>
/// A utility class for this mod.
/// </summary>
internal static class CRUtils
{
    #region delegates

    private static Lazy<Action<Rabbit, int>> CharacterTimerSetter = new(() =>
        typeof(Rabbit).GetCachedField("characterCheckTimer", ReflectionCache.FlagTypes.InstanceFlags)
        .GetInstanceFieldSetter<Rabbit, int>()
    );

    #endregion

    /// <summary>
    /// Checks to make sure it's safe to spawn butterflies.
    /// </summary>
    /// <param name="loc">Game location to check.</param>
    /// <returns>True if we should spawn butterflies, false otherwise.</returns>
    internal static bool ShouldSpawnButterflies([NotNullWhen(true)] this GameLocation? loc)
        => loc is not null && !Game1.isDarkOut()
            && (ModEntry.Config.ButterfliesSpawnInRain || !loc.IsOutdoors || !Game1.IsRainingHere(loc));

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

    internal static IEnumerable<(Vector2, bool)> FindBunnySpawnTile(GameLocation loc, Vector2 playerTile, int count)
    {
        if (loc.largeTerrainFeatures?.Count is null or 0)
        {
            yield break;
        }

        count *= ModEntry.Config.CritterSpawnMultiplier;
        foreach (LargeTerrainFeature? feature in loc.largeTerrainFeatures)
        {
            if (count <= 0)
            {
                yield break;
            }

            if (feature is Bush bush && Vector2.DistanceSquared(bush.tilePosition.Value, playerTile) <= 225)
            {
                if (bush.size.Value == Bush.walnutBush && bush.tileSheetOffset.Value == 1)
                {
                    // this is a walnut bush. Turns out bunnies can collect those.
                    continue;
                }

                bool flipped = Game1.random.Next(2) == 0;
                Vector2 startTile = bush.tilePosition.Value;
                int distance = Game1.random.Next(5, 12);

                for (int i = distance; i > 0; i--)
                {
                    Vector2 tile = startTile;
                    startTile.X += flipped ? 1 : -1;
                    if (!bush.getBoundingBox().Intersects(new Rectangle((int)startTile.X * 64, (int)startTile.Y * 64, 64, 64)) && !loc.isTileLocationTotallyClearAndPlaceable(startTile))
                    {
                        yield return (tile, flipped);
                        count--;
                        break;
                    }
                }
                yield return (startTile, flipped);
                count--;
            }
        }
    }

    internal static void SpawnRabbit(List<Critter>? critters, Vector2 tile, GameLocation loc, bool flipped)
    {
        if (critters is not null)
        {
            Rabbit rabbit = new Rabbit(tile, flipped);
            CharacterTimerSetter.Value(rabbit, 1000);
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