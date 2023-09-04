using Microsoft.Xna.Framework.Graphics;

namespace AtraCore.Framework.Caches.AssetCache;

/// <summary>
/// An asset holder for a texture.
/// </summary>
public sealed class TextureHolder : BaseAssetHolder<Texture2D>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextureHolder"/> class.
    /// </summary>
    /// <param name="assetName">the asset name to get.</param>
    public TextureHolder(IAssetName assetName)
        : base(assetName)
    {
    }

    /// <inheritdoc />
    public override Texture2D? Value
    {
        get
        {
            if (this.dirty || this.asset?.IsDisposed == true)
            {
                this.Refresh();
            }

            return this.asset?.IsDisposed == true ? null : this.asset;
        }
    }
}
