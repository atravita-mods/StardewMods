namespace AtraCore.Framework.Caches.AssetCache;

/// <summary>
/// A holder around an asset that is not processed.
/// </summary>
/// <typeparam name="TAsset">The type of the asset.</typeparam>
public class BaseAssetHolder<TAsset> : IAssetHolder<TAsset>
{
    public TAsset Value { get; }
    public IAssetName AssetName { get; }
}
