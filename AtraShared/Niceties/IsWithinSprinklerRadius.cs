using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using Microsoft.Xna.Framework;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace AtraShared.Niceties;

internal static class IsWithinSprinklerRadius
{
    internal readonly record struct Tile (string Map, Vector2 Pos);

    private static HashSet<Tile> wateredTiles = new();
    private static HashSet<string> processedMaps = new();

    // helpers
    private static IMonitor? Monitor;

    // APIs - check in order.
    private static IFlexibleSprinklersApi? flexibleSprinklersApi;
    private static ILineSprinklersApi? lineSprinklersApi;
    private static IBetterSprinklersApi? betterSprinklersApi;

    internal static void Init(IMonitor monitor, ITranslationHelper translation, IModRegistry registry)
    {
        Monitor = monitor;
        IntegrationHelper helper = new(monitor, translation, registry);
        _ = helper.TryGetAPI("Shockah.FlexibleSprinklers", "1.2.5", out flexibleSprinklersApi)
            || helper.TryGetAPI("hootless.LineSprinklers", "1.1.1", out lineSprinklersApi)
            || helper.TryGetAPI("Speeder.BetterSprinklers", "2.5.0", out betterSprinklersApi);
    }

    internal static void Reset()
    {
        wateredTiles.Clear();
        processedMaps.Clear();
    }

    internal static bool IsTileInWateringRange(GameLocation location, Vector2 pos)
    {
        if (location is null)
        {
            return false;
        }
        CalculateWateredTiles(location);
        return wateredTiles.Contains(new Tile(location.NameOrUniqueName, pos));
    }

    internal static bool IsTileInWateringRange(string locname, Vector2 pos)
        => IsTileInWateringRange(new Tile(locname, pos));

    internal static bool IsTileInWateringRange(Tile tile)
    {
        if (!processedMaps.Contains(tile.Map))
        {
            CalculateWateredTiles(Game1.getLocationFromName(tile.Map));
        }
        return wateredTiles.Contains(tile);
    }

    private static bool CalculateWateredTiles(GameLocation location)
    {
        if (location is null || processedMaps.Contains(location.NameOrUniqueName))
        {
            return false;
        }

        processedMaps.Add(location.NameOrUniqueName);

        if (flexibleSprinklersApi is not null)
        {
            foreach (Vector2 vec in flexibleSprinklersApi.GetAllTilesInRangeOfSprinklers(location))
            {
                if ((location.terrainFeatures.TryGetValue(vec, out TerrainFeature? terrain) && terrain is HoeDirt)
                    || (location.objects.TryGetValue(vec, out SObject? obj) && obj is IndoorPot pot && pot.hoeDirt?.Value is not null))
                {
                    wateredTiles.Add(new Tile(location.NameOrUniqueName, vec));
                }
            }
        }
        else
        {
            // If either better sprinkers or line sprinklers are installed
            // ask them for the relative sprinkler watered area.
            IDictionary<int, Vector2[]>? tilemap = lineSprinklersApi?.GetSprinklerCoverage()
                ?? betterSprinklersApi?.GetSprinklerCoverage();

            foreach (SObject obj in location.objects.Values)
            {
                IEnumerable<Vector2> tiles;
                if (tilemap?.TryGetValue(obj.ParentSheetIndex, out Vector2[]? vector2s) == true)
                { // got tile map from api, adjust from relative to absolute location.
                    tiles = vector2s.Select((v) => v + obj.TileLocation);
                }
                else
                { // default to vanilla logic.
                    tiles = obj.GetSprinklerTiles();
                }

                foreach (Vector2 vec in tiles)
                {
                    if (location.terrainFeatures.TryGetValue(vec, out TerrainFeature? terrain)
                        && terrain is HoeDirt)
                    {
                        wateredTiles.Add(new Tile(location.NameOrUniqueName, vec));
                    }
                }
            }
        }
        return true;
    }

}
