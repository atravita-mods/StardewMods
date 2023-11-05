namespace EastScarp;

using EastScarp.Models;

using StardewModdingAPI.Events;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static IAssetName locationExtendedModelLocation = null!;
    private static Lazy<Dictionary<string, Model>> _data = new(() => Game1.content.Load<Dictionary<string, Model>>(locationExtendedModelLocation.BaseName));

    private static IAssetName durationOverride = null!;

    /// <summary>
    /// Gets the assetname used to register emoji overrides.
    /// </summary>
    internal static IAssetName EmojiOverride { get; private set; } = null!;

    /// <summary>
    /// Gets the orders to make untimed.
    /// </summary>
    internal static Lazy<HashSet<string>> Untimed { get; private set; } = new(GetUntimed);

    /// <summary>
    /// The location extension data.
    /// </summary>
    internal static Dictionary<string, Model> Data => _data.Value;

    internal static void Init(IGameContentHelper parser)
    {
        locationExtendedModelLocation = parser.ParseAssetName("Mods/EastScarp/LocationMetadata");
    }

    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(locationExtendedModelLocation))
        {
            e.LoadFromModFile<Dictionary<string, Model>>("assets/data.json", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(EmojiOverride))
        {
            e.LoadFrom(static () => new Dictionary<string, EmojiData>(), AssetLoadPriority.Low);
        }
    }

    /// <inheritdoc cref="IContentEvents.AssetsInvalidated"/>
    internal static void Invalidate(IReadOnlySet<IAssetName>? assets)
    {
        if (assets is null || assets.Contains(locationExtendedModelLocation))
        {
            _data = new (() => Game1.content.Load<Dictionary<string, Model>>(locationExtendedModelLocation.BaseName));
        }
    }

    /// <summary>
    /// Gets the untiled special order quest keys I manage.
    /// </summary>
    /// <returns>Hashset of quest keys.</returns>
    private static HashSet<string> GetUntimed()
        => GetDurationOverride().Where(kvp => kvp.Value.AsSpan().Trim().Equals("-1", StringComparison.Ordinal))
                                .Select(kvp => kvp.Key).ToHashSet();
}
