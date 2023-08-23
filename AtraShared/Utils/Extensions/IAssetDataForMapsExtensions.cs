using Microsoft.Xna.Framework;

using xTile.Dimensions;

using XTile = xTile.Tiles.Tile;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extensions for IAssetDataForMaps.
/// </summary>
public static class IAssetDataForMapsExtensions
{
    /// <summary>
    /// Adds a tile property on a specific tile.
    /// </summary>
    /// <param name="map">map to add to.</param>
    /// <param name="monitor">logger instance.</param>
    /// <param name="layer">layer to grab from.</param>
    /// <param name="key">key.</param>
    /// <param name="property">value.</param>
    /// <param name="placementTile">tile to edit.</param>
    public static void AddTileProperty(this IAssetDataForMap map, IMonitor monitor, string layer, string key, string property, Vector2 placementTile)
    {
        XTile? tile = map.Data.GetLayer(layer)?.PickTile(
            new Location((int)placementTile.X * Game1.tileSize, (int)placementTile.Y * Game1.tileSize),
            Game1.viewport.Size);
        if (tile is null)
        {
            monitor.Log($"Tile could not be edited for {property}, please let atra know!", LogLevel.Warn);
            return;
        }
        tile.Properties[key] = property;
    }
}