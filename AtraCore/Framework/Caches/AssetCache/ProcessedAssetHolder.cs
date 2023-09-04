namespace AtraCore.Framework.Caches.AssetCache;

/// <summary>
/// Holds a reference to an asset that's processed.
/// </summary>
/// <typeparam name="TAsset">The type of the asset.</typeparam>
/// <typeparam name="TOutput">The post-processed type.</typeparam>
internal sealed class ProcessedAssetHolder<TAsset, TOutput> : IAssetHolder<TOutput>
{
    private readonly IAssetName assetName;
    private TOutput? cachedOutput;
    private Func<TAsset, TOutput> del;
    private bool dirty = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessedAssetHolder{TAsset, TOutput}"/> class.
    /// </summary>
    /// <param name="assetName">The asset name.</param>
    /// <param name="del">A function to process the output.</param>
    public ProcessedAssetHolder(IAssetName assetName, Func<TAsset, TOutput> del)
    {
        this.assetName = assetName;
        this.del = del;
        this.Refresh();
    }

    /// <inheritdoc />
    public TOutput? Value
    {
        get
        {
            if (this.dirty)
            {
                this.Refresh();
            }
            return this.cachedOutput;
        }
    }

    /// <inheritdoc />
    public IAssetName AssetName => this.assetName;

    /// <inheritdoc />
    public void MarkDirty() => this.dirty = true;

    /// <inheritdoc />
    public void Refresh()
    {
        var asset = Game1.content.Load<TAsset>(this.assetName.BaseName);
        this.cachedOutput = this.del(asset);
        this.dirty = false;
    }
}
