namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extension methods for IGameContentHelper.
/// </summary>
public static class GameContentHelperExtensions
{
    /// <summary>
    /// Invalidates both an asset and the locale-specific version of an asset.
    /// </summary>
    /// <param name="helper">The game content helper.</param>
    /// <param name="assetName">The (string) asset to invalidate.</param>
    /// <returns>if something was invalidated.</returns>
    public static bool InvalidateCacheAndLocalized(this IGameContentHelper helper, string assetName)
        => helper.InvalidateCache(assetName)
            | (helper.CurrentLocaleConstant != LocalizedContentManager.LanguageCode.en && helper.InvalidateCache(assetName + "." + helper.CurrentLocale));

    /// <summary>
    /// Invalidates both an asset and the locale-specific version of an asset.
    /// </summary>
    /// <param name="helper">The game content helper.</param>
    /// <param name="assetName">The (string) asset to invalidate.</param>
    /// <returns>if something was invalidated.</returns>
    public static bool InvalidateCacheAndLocalized(this IGameContentHelper helper, IAssetName assetName)
        => helper.InvalidateCache(assetName)
            | (helper.CurrentLocaleConstant != LocalizedContentManager.LanguageCode.en && helper.InvalidateCache(assetName.BaseName + "." + helper.CurrentLocale));
}
