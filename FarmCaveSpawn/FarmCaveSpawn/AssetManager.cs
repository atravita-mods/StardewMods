using StardewModdingAPI.Utilities;

namespace FarmCaveSpawn;

/// <summary>
/// Handles the fake assets for this mod.
/// </summary>
internal class AssetManager : IAssetLoader
{
    private AssetManager()
    {
    }

    /// <summary>
    /// Gets the instance of the assetmanager for this mod.
    /// </summary>
    public static AssetManager Instance { get; } = new();

    /// <summary>
    /// Gets fake asset location for the denylist.
    /// </summary>
    public static string DENYLIST_LOCATION { get; } = PathUtilities.NormalizeAssetName("Mods/atravita_FarmCaveSpawn_denylist");

    /// <summary>
    /// Gets fake asset location for more locations that can spawn in fruit.
    /// </summary>
    public static string ADDITIONAL_LOCATIONS_LOCATION { get; } = PathUtilities.NormalizeAssetName("Mods/atravita_FarmCaveSpawn_additionalLocations");

    /// <inheritdoc/>
    public bool CanLoad<T>(IAssetInfo asset)
        => asset.AssetNameEquals(DENYLIST_LOCATION) || asset.AssetNameEquals(ADDITIONAL_LOCATIONS_LOCATION);

    /// <inheritdoc/>
    public T Load<T>(IAssetInfo asset)
    {
        if (asset.AssetNameEquals(DENYLIST_LOCATION))
        {
            return (T)(object)new Dictionary<string, string>
            {
            };
        }
        else if (asset.AssetNameEquals(ADDITIONAL_LOCATIONS_LOCATION))
        {
            return (T)(object)new Dictionary<string, string>
            {
                ["FlashShifter.SVECode"] = "Custom_MinecartCave, Custom_DeepCave",
#if DEBUG // Regex's test!
                ["atravita.FarmCaveSpawn"] = "Town:[(4;5);(34;40)]",
#endif
            };
        }
        throw new InvalidOperationException($"Should not have tried to load '{asset.AssetName}'.");
    }
}
