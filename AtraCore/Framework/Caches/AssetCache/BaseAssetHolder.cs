namespace AtraCore.Framework.Caches.AssetCache;

/// <summary>
/// A holder around an asset that is not processed.
/// </summary>
/// <typeparam name="TAsset">The type of the asset.</typeparam>
public class BaseAssetHolder<TAsset> : IAssetHolder<TAsset>
    where TAsset : class
{
    private readonly IAssetName assetName;
    private readonly object lockObj = new();

    /// <summary>
    /// Whether the asset should be refreshed before it's accessed again.
    /// </summary>
    private protected volatile bool dirty = false;

    /// <summary>
    /// The backing field for the asset itself.
    /// </summary>
    private protected TAsset? asset;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseAssetHolder{TAsset}"/> class.
    /// </summary>
    /// <param name="assetName">The name of the asset.</param>
    public BaseAssetHolder(IAssetName assetName)
    {
        this.assetName = assetName;
        this.Refresh();
    }

    /// <inheritdoc />
    public virtual TAsset? Value
    {
        get
        {
            if (this.dirty)
            {
                this.Refresh();
            }

            return this.asset;
        }
    }

    /// <inheritdoc />
    public virtual IAssetName AssetName => this.assetName;

    /// <inheritdoc />
    public void MarkDirty() => this.dirty = true;

    /// <inheritdoc />
    public virtual void Refresh()
    {
        lock (this.lockObj)
        {
            this.asset = Game1.content.Load<TAsset>(this.assetName.BaseName);
            this.dirty = false;
        }
    }
}
