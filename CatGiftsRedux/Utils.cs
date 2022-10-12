using AtraCore.Framework.QueuePlayerAlert;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;

using StardewValley.Characters;

namespace CatGiftsRedux;

/// <summary>
/// A utility class for this mod.
/// </summary>
internal static class Utils
{

    /// <summary>
    /// Gets a random empty tile on a map.
    /// </summary>
    /// <param name="location">The game location to get a random tile from.</param>
    /// <param name="tries">How many times to try.</param>
    /// <returns>Empty tile, or null to indicate failure.</returns>
    internal static Vector2? GetRandomTileImpl(this GameLocation location, int tries = 10)
    {
        do
        {
            var tile = location.getRandomTile();
            if (location.isWaterTile((int)tile.X, (int)tile.Y))
            {
                continue;
            }

            var options = Utility.recursiveFindOpenTiles(location, tile, 1);
            if (options.Count > 0)
            {
                return options[0];
            }
        }
        while (tries-- > 0);

        return null;
    }

    /// <summary>
    /// Places the item at the specified tile, and alerts the player.
    /// </summary>
    /// <param name="location">Map.</param>
    /// <param name="tile">Tile to attempt.</param>
    /// <param name="item">Item to place.</param>
    /// <param name="pet">Pet to credit.</param>
    internal static void PlaceItem(this GameLocation location, Vector2 tile, Item item, Pet pet)
    {
        ModEntry.ModMonitor.DebugOnlyLog($"Placing {item.DisplayName} at {location.NameOrUniqueName} - {tile}");

        PlayerAlertHandler.AddMessage(
            message: new($"{pet.Name} has brought you a {item.DisplayName}", Color.PaleGreen, 500, true),
            soundCue: pet is Cat ? "Cowboy_Footstep" : "dog_pant");

        if (item is SObject obj && !location.Objects.ContainsKey(tile))
        {
            obj.IsSpawnedObject = true;
            location.Objects[tile] = obj;
        }
        else
        {
            var debris = new Debris(item, tile * 64f);
            location.debris.Add(debris);
        }
    }
}