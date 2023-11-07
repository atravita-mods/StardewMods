namespace AtraCore.Framework.Caches.AssetCache;

/// <summary>
/// A holder for an asset.
/// </summary>
/// <typeparam name="TOutput">The type of the output.</typeparam>
public interface IAssetHolder<out TOutput>
{
    /// <summary>
    /// Gets the value held by this AssetHolder, or null if it's (temporarily) not available.
    /// </summary>
    public TOutput? Value { get; }

    /// <summary>
    /// Gets the asset name associated with this asset.
    /// </summary>
    public IAssetName AssetName { get; }

    /// <summary>
    /// Calls the asset again.
    /// </summary>
    public void Refresh();

    /// <summary>
    /// Marks the asset as dirty, to be refreshed the next time it's used.
    /// </summary>
    public void MarkDirty();
}
