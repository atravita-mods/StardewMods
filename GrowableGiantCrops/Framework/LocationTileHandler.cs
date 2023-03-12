using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;

using StardewValley.Locations;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// Handles tile-specific changes for the shove mod.
/// </summary>
internal static class LocationTileHandler
{
    private static readonly Dictionary<string, LocationTileDelegate> Handlers = new()
    {
        ["IslandNorth"] = IslandNorthHandler,
    };

    private delegate bool LocationTileDelegate(
        GameLocation location,
        Vector2 tile);

    internal static bool ApplyShovelToMap(GameLocation location, Vector2 tile)
    {
        if (Handlers.TryGetValue(location.NameOrUniqueName, out LocationTileDelegate? handler))
        {
            ModEntry.ModMonitor.DebugOnlyLog($"Running handler for {location.NameOrUniqueName}", LogLevel.Info);
            return handler(location, tile);
        }
        return false;
    }

    private static bool IslandNorthHandler(GameLocation location, Vector2 tile)
    {
        if (location is not IslandNorth islandNorth || tile.Y != 47f || (tile.X != 21f && tile.X != 22f) || islandNorth.caveOpened.Value)
        {
            return false;
        }

        islandNorth.caveOpened.Value = true;
        ShovelTool.AddAnimations(location, tile, Game1.mouseCursors2Name, new Rectangle(155, 224, 32, 32), new Point(2, 2));

        return true;
    }
}
