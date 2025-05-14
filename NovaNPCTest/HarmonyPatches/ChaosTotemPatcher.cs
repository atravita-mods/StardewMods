namespace NovaNPCTest.HarmonyPatches;

using HarmonyLib;

using Microsoft.Xna.Framework;
using StardewValley.Extensions;
using System.Linq;

[HarmonyPatch(typeof(SObject))]
internal static class ChaosTotemPatcher
{
    [HarmonyPatch(nameof(SObject.performUseAction))]
    private static bool Prefix(SObject __instance, GameLocation location)
    {
        bool normal_gameplay = !Game1.eventUp && !Game1.isFestival() && !Game1.fadeToBlack && !Game1.player.swimming.Value && !Game1.player.bathingClothes.Value && !Game1.player.onBridge.Value;

        if (normal_gameplay && __instance.QualifiedItemId == "(O)TenebrousNova.EliDylan.CP_TenebrousNova.EnD.ChaosTotem")
        {
            ChaosTotemData data = GetRandomChaosTotemData(location);

            Game1.player.jitterStrength = 1f;
            Color sprinkleColor = Utility.StringToColor(data.Color) ?? Color.White;
            location.playSound("warrior");
            Game1.player.faceDirection(2);
            Game1.player.CanMove = false;
            Game1.player.temporarilyInvincible = true;
            Game1.player.temporaryInvincibilityTimer = -4000;
            Game1.changeMusicTrack("silence");
            Game1.player.FarmerSprite.animateOnce([
                new (57, 2000, secondaryArm: false, flip: false),
                new ((short)Game1.player.FarmerSprite.CurrentFrame, 0, secondaryArm: false, flip: false, (Farmer who) => DoWarp(who, data, location), behaviorAtEndOfFrame: true)
            ]);
            WarpAnimation(__instance, location);
            return false;
        }

        return true;
    }

    private static void WarpAnimation(SObject obj, GameLocation location)
    {
        TemporaryAnimatedSprite potion = new(0, 9999f, 1, 999, Game1.player.Position + new Vector2(0f, -124f), flicker: false, flipped: false, verticalFlipped: false, 0f)
        {
            alpha = 1f,
            layerDepth = 0.9f,
        };
        potion.CopyAppearanceFromItemId(obj.QualifiedItemId);
        TemporaryAnimatedSprite sprite = new(0, 9999f, 1, 999, Game1.player.Position + new Vector2(0f, -96f), flicker: false, flipped: false, verticalFlipped: false, 0f)
        {
            motion = new Vector2(0f, -1f),
            scaleChange = 0.01f,
            alpha = 1f,
            alphaFade = 0.0075f,
            shakeIntensity = 1f,
            initialPosition = Game1.player.Position + new Vector2(0f, -96f),
            xPeriodic = true,
            xPeriodicLoopTime = 1000f,
            xPeriodicRange = 4f,
            layerDepth = 1f,
        };
        sprite.CopyAppearanceFromItemId(obj.QualifiedItemId);
        Game1.Multiplayer.broadcastSprites(location, sprite);
        sprite = new TemporaryAnimatedSprite(0, 9999f, 1, 999, Game1.player.Position + new Vector2(-64f, -96f), flicker: false, flipped: false, verticalFlipped: false, 0f)
        {
            motion = new Vector2(0f, -0.5f),
            scaleChange = 0.005f,
            scale = 0.5f,
            alpha = 1f,
            alphaFade = 0.0075f,
            shakeIntensity = 1f,
            delayBeforeAnimationStart = 10,
            initialPosition = Game1.player.Position + new Vector2(-64f, -96f),
            xPeriodic = true,
            xPeriodicLoopTime = 1000f,
            xPeriodicRange = 4f,
            layerDepth = 0.9999f,
        };
        sprite.CopyAppearanceFromItemId(obj.QualifiedItemId);
        Game1.Multiplayer.broadcastSprites(location, sprite);
        sprite = new TemporaryAnimatedSprite(0, 9999f, 1, 999, Game1.player.Position + new Vector2(64f, -96f), flicker: false, flipped: false, verticalFlipped: false, 0f)
        {
            motion = new Vector2(0f, -0.5f),
            scaleChange = 0.005f,
            scale = 0.5f,
            alpha = 1f,
            alphaFade = 0.0075f,
            delayBeforeAnimationStart = 20,
            shakeIntensity = 1f,
            initialPosition = Game1.player.Position + new Vector2(64f, -96f),
            xPeriodic = true,
            xPeriodicLoopTime = 1000f,
            xPeriodicRange = 4f,
            layerDepth = 0.9988f,
        };
        sprite.CopyAppearanceFromItemId(obj.QualifiedItemId);
        Game1.Multiplayer.broadcastSprites(location, potion);
        Game1.Multiplayer.broadcastSprites(location, sprite);
        Utility.addSprinklesToLocation(location, Game1.player.TilePoint.X, Game1.player.TilePoint.Y, 16, 16, 1300, 20, Color.White, null, motionTowardCenter: true);
        DelayedAction.removeTemporarySpriteAfterDelay(location, potion.id, 2000);
    }

    private static ChaosTotemData GetRandomChaosTotemData(GameLocation location)
    {
        ChaosTotemData data = null;

        while (data == null || data.Location == location.NameOrUniqueName)
        {
            data = ChaosTotemAsset.Asset[Game1.random.ChooseFrom([.. ChaosTotemAsset.Asset.Keys])];
        }

        return data;
    }

    private static void DoWarp(Farmer who, ChaosTotemData data, GameLocation location)
    {
        for (int i = 0; i < 12; i++)
        {
            Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(354, Game1.random.Next(25, 75), 6, 1, new Vector2(Game1.random.Next((int)who.Position.X - 256, (int)who.Position.X + 192), Game1.random.Next((int)who.Position.Y - 256, (int)who.Position.Y + 192)), flicker: false, Game1.random.NextBool()));
        }
        who.playNearbySoundAll("wand");
        Game1.displayFarmer = false;
        Game1.player.temporarilyInvincible = true;
        Game1.player.temporaryInvincibilityTimer = -2000;
        Game1.player.freezePause = 1000;
        Game1.flashAlpha = 1f;
        DelayedAction.fadeAfterDelay(() => ActuallyDoWarp(data), 1000);
        Rectangle playerBounds = who.GetBoundingBox();
        new Rectangle(playerBounds.X, playerBounds.Y, 64, 64).Inflate(192, 192);
        int j = 0;
        Point playerTile = who.TilePoint;
        for (int x = playerTile.X + 8; x >= playerTile.X - 8; x--)
        {
            Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(6, new Vector2(x, playerTile.Y) * 64f, Color.White, 8, flipped: false, 50f)
            {
                layerDepth = 1f,
                delayBeforeAnimationStart = j * 25,
                motion = new Vector2(-0.25f, 0f),
            });
            j++;
        }
    }

    private static void ActuallyDoWarp(ChaosTotemData data)
    {
        if (Game1.player.IsLocalPlayer)
        {
            Game1.warpFarmer(data.Location, (int)data.Position.X, (int)data.Position.Y, Game1.player.FacingDirection);
            Game1.fadeToBlackAlpha = 0.99f;
            Game1.screenGlow = false;
            Game1.player.temporarilyInvincible = false;
            Game1.player.temporaryInvincibilityTimer = 0;
            Game1.displayFarmer = true;
        }
    }
}
