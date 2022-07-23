using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace GiantCropFertilizer;
internal static class GCFUtils
{
    internal static void FixIDsInLocation(this GameLocation loc, int storedID, int newID)
    {
        foreach (TerrainFeature terrainfeature in loc.terrainFeatures.Values)
        {
            if (terrainfeature is HoeDirt dirt && dirt.fertilizer.Value == storedID)
            {
                dirt.fertilizer.Value = newID;
            }
        }
        foreach (SObject obj in loc.Objects.Values)
        {
            if (obj is IndoorPot pot && pot.hoeDirt?.Value?.fertilizer?.Value == storedID)
            {
                pot.hoeDirt.Value.fertilizer.Value = newID;
            }
        }
    }
}
