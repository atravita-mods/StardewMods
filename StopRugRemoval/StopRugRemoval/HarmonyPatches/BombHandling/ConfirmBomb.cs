using System.Reflection;
using AtraBase.Toolkit.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;

namespace StopRugRemoval.HarmonyPatches.BombHandling;

[HarmonyPatch]
internal static class ConfirmBomb
{

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (Type t in typeof(GameLocation).GetAssignableTypes(publiconly: true, includeAbstract: false))
        {
            if (AccessTools.Method(t, nameof(GameLocation.answerDialogueAction), new Type[] { typeof(string), typeof(string[]) }) is MethodBase method
                && method.DeclaringType == t)
            {
                yield return method;
            }
        }
    }

    internal static bool Prefix(GameLocation __instance, string __0, string[] __1)
    {
        try
        {
            switch (__0)
            {
                case "atravitaInteractionTweaksBombs_BombsArea":
                    SObjectPatches.HaveConfirmed.Value = true;
                    goto case "atravitaInteractionTweaksBombs_BombsYes";
                case "atravitaInteractionTweaksBombs_BombsYes":
                    Game1.player.reduceActiveItemByOne();
                    ExplodeBomb(__instance, SObjectPatches.whichBomb.Value, SObjectPatches.BombLocation.Value);
                    break;
                case "atravitaInteractionTweaksBombs_BombsNo":
                    break;
                default:
                    return true;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into issues in prefix for confirming bombs.\n\n{ex}", LogLevel.Error);
        }
        return true;
    }

    /// <summary>
    /// The code in this function is effectively copied from the game, and explodes a bomb on this tile.
    /// </summary>
    /// <param name="loc">Location to explode bomb.</param>
    /// <param name="whichBomb">Which bomb to explode.</param>
    /// <param name="tileloc">Tile to explode bomb on.</param>
    internal static void ExplodeBomb(GameLocation loc, int whichBomb, Vector2 tileloc)
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
        Multiplayer mp = ModEntry.MultiPlayer;
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
        ModEntry.MultiPlayer.broadcastSprites(loc, tas_white);
        loc.netAudio.StartPlaying("fuse");
    }
}