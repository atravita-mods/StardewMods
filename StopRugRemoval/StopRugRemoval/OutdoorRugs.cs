using AtraBase.Collections;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Locations;
using StardewValley.Objects;

namespace StopRugRemoval;

/// <summary>
/// Handles applying and removing NoSpawn from each rug's tile.
/// </summary>
internal static class OutdoorRugs
{
    private static readonly DefaultDict<string, List<Vector2>> noSpawns = new(() => new List<Vector2>());

    internal static void ApplyNoSpawns()
    {
        ModEntry.ModMonitor.DebugLog("Applying NoSpawns", LogLevel.Alert);
        foreach (GameLocation gameLocation in Game1.locations)
        {
            foreach (Furniture furniture in gameLocation.furniture)
            {
                if (furniture.furniture_type.Value != Furniture.rug)
                {
                    continue;
                }
                Rectangle bounds = furniture.boundingBox.Value;
                (int tilex, int tiley) = furniture.TileLocation.ToPoint();
                for (int x = 0; x < bounds.Width / 64; x++)
                {
                    for (int y = 0; y < bounds.Height / 64; y++)
                    {
                        noSpawns[gameLocation.NameOrUniqueName].Add(furniture.TileLocation);
                        ModEntry.ModMonitor.DebugLog($"{gameLocation.NameOrUniqueName}, {furniture.TileLocation}");
                    }
                }
            }
        }
    }

    internal static void RemoveNoSpawns()
    {

    }

}

