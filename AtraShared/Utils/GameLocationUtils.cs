using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using StardewValley.Locations;

namespace AtraShared.Utils;

// TODO: Remove checks for BuildableGameLocation in 1.6.

/// <summary>
/// Utility for gamelocations.
/// </summary>
internal static class GameLocationUtils
{
    /// <summary>
    /// The code in this function is effectively copied from the game, and explodes a bomb on this tile.
    /// </summary>
    /// <param name="loc">Location to explode bomb.</param>
    /// <param name="whichBomb">Which bomb to explode.</param>
    /// <param name="tileloc">Tile to explode bomb on.</param>
    /// <param name="mp">Multiplayer instance - used to broadcast sprites.</param>
    internal static void ExplodeBomb(GameLocation loc, int whichBomb, Vector2 tileloc, Multiplayer mp)
    {
        int bombID = Game1.random.Next();
        loc.playSound("thudStep");
        TemporaryAnimatedSprite tas_bomb = new(
            initialParentTileIndex: whichBomb,
            animationInterval: 100f,
            animationLength: 1,
            numberOfLoops: 24,
            position: tileloc,
            flicker: true,
            flipped: false,
            parent: loc,
            owner: Game1.player)
        {
            shakeIntensity = 0.5f,
            shakeIntensityChange = 0.002f,
            extraInfoForEndBehavior = bombID,
            endFunction = loc.removeTemporarySpritesWithID,
        };
        mp.broadcastSprites(loc, tas_bomb);
        TemporaryAnimatedSprite tas_yellow = new(
            textureName: "LooseSprites\\Cursors",
            sourceRect: new Rectangle(598, 1279, 3, 4),
            animationInterval: 53f,
            animationLength: 5,
            numberOfLoops: 9,
            position: tileloc + (new Vector2(5f, 3f) * 4f),
            flicker: true,
            flipped: false,
            layerDepth: (float)(tileloc.Y + 7) / 10000f,
            alphaFade: 0f,
            color: Color.Yellow,
            scale: 4f,
            scaleChange: 0f,
            rotation: 0f,
            rotationChange: 0f)
        {
            id = bombID,
        };
        mp.broadcastSprites(loc, tas_yellow);
        TemporaryAnimatedSprite tas_orange = new(
            textureName: "LooseSprites\\Cursors",
            sourceRect: new Rectangle(598, 1279, 3, 4),
            animationInterval: 53f,
            animationLength: 5,
            numberOfLoops: 9,
            position: tileloc + (new Vector2(5f, 3f) * 4f),
            flicker: true,
            flipped: false,
            layerDepth: (float)(tileloc.Y + 7) / 10000f,
            alphaFade: 0f,
            color: Color.Orange,
            scale: 4f,
            scaleChange: 0f,
            rotation: 0f,
            rotationChange: 0f)
        {
            delayBeforeAnimationStart = 100,
            id = bombID,
        };
        mp.broadcastSprites(loc, tas_orange);
        TemporaryAnimatedSprite tas_white = new(
            textureName: "LooseSprites\\Cursors",
            sourceRect: new Rectangle(598, 1279, 3, 4),
            animationInterval: 53f,
            animationLength: 5,
            numberOfLoops: 9,
            position: tileloc + (new Vector2(5f, 3f) * 4f),
            flicker: true,
            flipped: false,
            layerDepth: (float)(tileloc.Y + 7) / 10000f,
            alphaFade: 0f,
            color: Color.White,
            scale: 4f,
            scaleChange: 0f,
            rotation: 0f,
            rotationChange: 0f)
        {
            delayBeforeAnimationStart = 200,
            id = bombID,
        };
        mp.broadcastSprites(loc, tas_white);
        loc.netAudio.StartPlaying("fuse");
    }

    /// <summary>
    /// Yields all game locations.
    /// </summary>
    /// <returns>IEnumerable of all game locations.</returns>
    internal static IEnumerable<GameLocation> YieldAllLocations()
    {
        foreach (GameLocation location in Game1.locations)
        {
            yield return location;
            if (location is BuildableGameLocation buildableloc)
            {
                foreach (GameLocation loc in YieldInteriorLocations(buildableloc))
                {
                    yield return loc;
                }
            }
        }
    }

    private static IEnumerable<GameLocation> YieldInteriorLocations(BuildableGameLocation loc)
    {
        foreach (Building building in loc.buildings)
        {
            if (building.indoors?.Value is GameLocation indoorloc)
            {
                yield return indoorloc;
                if (indoorloc is BuildableGameLocation buildableIndoorLoc)
                {
                    foreach (GameLocation nestedLocation in YieldInteriorLocations(buildableIndoorLoc))
                    {
                        yield return nestedLocation;
                    }
                }
            }
        }
    }
}