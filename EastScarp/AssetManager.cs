namespace EastScarp;

using EastScarp.Models;

using StardewModdingAPI.Events;

using StardewValley.GameData.Characters;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static IAssetName locationExtendedModelLocation = null!;
    private static Lazy<Dictionary<string, LocationDataModel>> data = new (static () => Game1.content.Load<Dictionary<string, LocationDataModel>>(locationExtendedModelLocation.BaseName));

    private static IAssetName durationOverride = null!;

    /// <summary>
    /// Gets the assetname used to register emoji overrides.
    /// </summary>
    internal static IAssetName EmojiOverride { get; private set; } = null!;

    /// <summary>
    /// Gets the orders to make untimed.
    /// </summary>
    internal static Lazy<HashSet<string>> Untimed { get; private set; } = new (GetUntimed);

    /// <summary>
    /// Gets the location extension data.
    /// </summary>
    internal static Dictionary<string, LocationDataModel> Data => data.Value;

    /// <summary>
    /// Initializes this asset manager.
    /// </summary>
    /// <param name="parser">game content helper.</param>
    internal static void Init(IGameContentHelper parser)
    {
        locationExtendedModelLocation = parser.ParseAssetName("Mods/EastScarp/LocationMetadata");
        durationOverride = parser.ParseAssetName("Mods/EastScarp/DurationOverride");
        EmojiOverride = parser.ParseAssetName("Mods/EastScarp/EmojiOverride");
    }

    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(locationExtendedModelLocation))
        {
            e.LoadFromModFile<Dictionary<string, LocationDataModel>>("assets/location_data.json", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(EmojiOverride))
        {
            e.LoadFromModFile<Dictionary<string, EmojiData>>("assets/emoji_data.json", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(durationOverride))
        {
            e.LoadFromModFile<Dictionary<string, string>>("assets/duration_override.json", AssetLoadPriority.Exclusive);
        }
    }

    /// <inheritdoc cref="IContentEvents.AssetsInvalidated"/>
    internal static void Invalidate(IReadOnlySet<IAssetName>? assets)
    {
        if (assets is null || assets.Contains(locationExtendedModelLocation))
        {
            data = new (static () => Game1.content.Load<Dictionary<string, LocationDataModel>>(locationExtendedModelLocation.BaseName));
        }
        if (Untimed.IsValueCreated && (assets is null || assets.Contains(durationOverride)))
        {
            Untimed = new (GetUntimed);
        }
    }

    /// <summary>
    /// Gets the duration override dictionary.
    /// </summary>
    /// <returns>The duration override dictionary.</returns>
    internal static Dictionary<string, string> GetDurationOverride()
        => Game1.content.Load<Dictionary<string, string>>(durationOverride.BaseName);

    /// <summary>
    /// Gets the untiled special order quest keys I manage.
    /// </summary>
    /// <returns>Hashset of quest keys.</returns>
    private static HashSet<string> GetUntimed()
        => GetDurationOverride().Where(kvp => kvp.Value.AsSpan().Trim().Equals("-1", StringComparison.Ordinal))
                                .Select(kvp => kvp.Key).ToHashSet();
}
