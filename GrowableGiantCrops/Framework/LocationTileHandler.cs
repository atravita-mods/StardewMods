using AtraCore.Utilities;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;

using StardewValley.Locations;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// Handles tile-specific changes for the shove mod.
/// </summary>
internal static class LocationTileHandler
{
    private static readonly Dictionary<string, List<LocationTileDelegate>> Handlers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["IslandNorth"] = new() { IslandNorthHandler },
        ["Railroad"] = new() { RailRoadHandler },
        ["Sewer"] = new() { SewerHandler },
    };

    private delegate bool LocationTileDelegate(
        ShovelTool shovel,
        Farmer who,
        GameLocation location,
        Vector2 tile);

    /// <summary>
    /// Applies the shovel to a specific map.
    /// </summary>
    /// <param name="shovel">The shovel instance.</param>
    /// <param name="who">The farmer doing the action.</param>
    /// <param name="location">Game location to apply to.</param>
    /// <param name="tile">Tile to apply to.</param>
    /// <returns>True if successfully applied, false otherwise.</returns>
    internal static bool ApplyShovelToMap(ShovelTool shovel, Farmer who, GameLocation location, Vector2 tile)
    {
        if (Handlers.TryGetValue(location.NameOrUniqueName, out List<LocationTileDelegate>? handlers))
        {
            ModEntry.ModMonitor.DebugOnlyLog($"Running handler for {location.NameOrUniqueName}", LogLevel.Info);
            return handlers.Any(handler => handler.Invoke(shovel, who, location, tile));
        }
        return false;
    }

    private static bool IslandNorthHandler(ShovelTool shovel, Farmer who, GameLocation location, Vector2 tile)
    {
        if (location is not IslandNorth islandNorth || tile.Y != 47f || (tile.X != 21f && tile.X != 22f) || islandNorth.caveOpened.Value)
        {
            return false;
        }

        islandNorth.caveOpened.Value = true;
        ShovelTool.AddAnimations(location, tile, Game1.mouseCursors2Name, new Rectangle(155, 224, 32, 32), new Point(2, 2));

        return true;
    }

    private static bool RailRoadHandler(ShovelTool shovel, Farmer who, GameLocation location, Vector2 tile)
    {
        if (location is not Railroad railroad)
        {
            return false;
        }

        string? property = railroad.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Action", "Buildings");
        if (property == "SummitBoulder")
        {
            Game1.drawObjectDialogue(I18n.Summit_Boulder());
            return true;
        }

        if (!railroad.witchStatueGone.Value && property == "WitchCaveBlock")
        {
            ModEntry.ModMonitor.Log($"Removing witch block.");
            Game1.playSound("cacklingWitch");

            // copied from Railroad.checkAction
            who.freezePause = 7000;
            DelayedAction.playSoundAfterDelay("fireball", 200);
            DelayedAction.removeTemporarySpriteAfterDelay(railroad, 9999f, 2000);
            for (int i = 0; i < 22; i++)
            {
                DelayedAction.playSoundAfterDelay("batFlap", 2220 + (240 * i));
            }
            Multiplayer multi = MultiplayerHelpers.GetMultiplayer();
            multi.broadcastSprites(railroad, new TemporaryAnimatedSprite(
                textureName: Game1.mouseCursorsName,
                new Rectangle(576, 271, 28, 31),
                animationInterval: 60f,
                animationLength: 3,
                numberOfLoops: 999,
                position: (new Vector2(54f, 34f) * Game1.tileSize) + (new Vector2(-2f, 1f) * Game1.pixelZoom),
                flicker: false,
                flipped: false,
                layerDepth: 0.2176f,
                alphaFade: 0f,
                color: Color.White,
                scale: Game1.pixelZoom,
                scaleChange: 0f,
                rotation: 0f,
                rotationChange: 0f)
            {
                xPeriodic = true,
                xPeriodicLoopTime = 8000f,
                xPeriodicRange = 384f,
                motion = new Vector2(-2f, 0f),
                acceleration = new Vector2(0f, -0.015f),
                pingPong = true,
                delayBeforeAnimationStart = 2000,
            });
            multi.broadcastSprites(railroad, new TemporaryAnimatedSprite(Game1.mouseCursorsName, new Rectangle(0, 499, 10, 11), 50f, 7, 999, new Vector2(54f, 34f) * 64f + new Vector2(7f, 11f) * 4f, flicker: false, flipped: false, 0.2177f, 0f, Color.White, 4f, 0f, 0f, 0f)
            {
                xPeriodic = true,
                xPeriodicLoopTime = 8000f,
                xPeriodicRange = 384f,
                motion = new Vector2(-2f, 0f),
                acceleration = new Vector2(0f, -0.015f),
                delayBeforeAnimationStart = 2000,
            });
            multi.broadcastSprites(railroad, new TemporaryAnimatedSprite(Game1.mouseCursorsName, new Rectangle(0, 499, 10, 11), 35.715f, 7, 8, new Vector2(54f, 34f) * 64f + new Vector2(3f, 10f) * 4f, flicker: false, flipped: false, 0.2305f, 0f, Color.White, 4f, 0f, 0f, 0f)
            {
                id = 9999f,
            });

            DelayedAction.playSoundAfterDelay("secret1", 2000);
            railroad.witchStatueGone.Value = true;
            if (!who.mailReceived.Contains("witchStatueGone"))
            {
                who.mailReceived.Add("witchStatueGone");
            }

            return true;
        }

        return false;
    }

    private static bool SewerHandler(ShovelTool shovel, Farmer who, GameLocation location, Vector2 tile)
    {
        if (location is not Sewer sewer || who.mailReceived.Contains("krobusUnseal"))
        {
            return false;
        }

        string? property = sewer.doesTileHaveProperty((int)tile.X, (int)tile.Y, "TouchAction", "Back");

        if (property == "MagicSeal")
        {

        }

        return false;
    }
}
