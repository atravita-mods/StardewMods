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
    };

    private delegate bool LocationTileDelegate(
        ShovelTool shovel,
        GameLocation location,
        Vector2 tile);

    /// <summary>
    /// Applies the shovel to a specific map.
    /// </summary>
    /// <param name="shovel">The shovel instance.</param>
    /// <param name="location">Game location to apply to.</param>
    /// <param name="tile">Tile to apply to.</param>
    /// <returns>True if successfully applied, false otherwise.</returns>
    internal static bool ApplyShovelToMap(ShovelTool shovel, GameLocation location, Vector2 tile)
    {
        if (Handlers.TryGetValue(location.NameOrUniqueName, out List<LocationTileDelegate>? handlers))
        {
            ModEntry.ModMonitor.DebugOnlyLog($"Running handler for {location.NameOrUniqueName}", LogLevel.Info);
            return handlers.Any(handler => handler.Invoke(shovel, location, tile));
        }
        return false;
    }

    private static bool IslandNorthHandler(ShovelTool shovel, GameLocation location, Vector2 tile)
    {
        if (location is not IslandNorth islandNorth || tile.Y != 47f || (tile.X != 21f && tile.X != 22f) || islandNorth.caveOpened.Value)
        {
            return false;
        }

        islandNorth.caveOpened.Value = true;
        ShovelTool.AddAnimations(location, tile, Game1.mouseCursors2Name, new Rectangle(155, 224, 32, 32), new Point(2, 2));

        return true;
    }

    private static bool RailRoadHandler(ShovelTool shovel, GameLocation location, Vector2 tile)
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

        if (property == "WitchCaveBlock")
        {
            ModEntry.ModMonitor.Log($"Removing witch block.");
            Game1.playSound("cacklingWitch");
        }

        return false;
    }
}
