using StardewModdingAPI.Utilities;

namespace FarmCaveSpawn;

internal class AssetManager : IAssetLoader
{
    public readonly string DENYLIST_LOCATION = PathUtilities.NormalizeAssetName("Mods/atravita_FarmCaveSpawn_denylist");
    public readonly string ADDITIONAL_LOCATIONS_LOCATION = PathUtilities.NormalizeAssetName("Mods/atravita_FarmCaveSpawn_additionalLocations");

    /// <inheritdoc/>
    public bool CanLoad<T>(IAssetInfo asset)
    {
        return asset.AssetNameEquals(this.DENYLIST_LOCATION) || asset.AssetNameEquals(this.ADDITIONAL_LOCATIONS_LOCATION);
    }

    /// <inheritdoc/>
    public T Load<T>(IAssetInfo asset)
    {
        if (asset.AssetNameEquals(this.DENYLIST_LOCATION))
        {
            return (T)(object)new Dictionary<string, string>
            {
            };
        }
        else if (asset.AssetNameEquals(this.ADDITIONAL_LOCATIONS_LOCATION))
        {
            return (T)(object)new Dictionary<string, string>
            {
                ["FlashShifter.SVECode"] = "Custom_MinecartCave, Custom_DeepCave"
#if DEBUG
                , ["atravita.FarmCaveSpawn"] = "Town:[(4;5);(34;40)]"
#endif
            };
        }
        throw new InvalidOperationException($"Should not have tried to load '{asset.AssetName}'.");
    }
}
