﻿using Microsoft.Xna.Framework.Graphics;

namespace GrowableGiantCrops.Framework.Assets;

/// <summary>
/// Holds a single texture asset.
/// </summary>
internal sealed class AssetHolder
{
    private readonly IAssetName? assetName;
    private Texture2D texture;
    private bool dirty = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetHolder"/> class.
    /// </summary>
    /// <param name="assetName">the name of the asset.</param>
    /// <param name="texture">A texture associated with the asset, or null to load it.</param>
    internal AssetHolder(IAssetName assetName, Texture2D? texture = null)
    {
        this.assetName = assetName;
        this.texture = texture ?? Game1.content.Load<Texture2D>(assetName.BaseName);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetHolder"/> class.
    /// </summary>
    /// <param name="texture">The texture to hold.</param>
    internal AssetHolder(Texture2D texture)
    {
        this.texture = texture;
    }

    /// <summary>
    /// Gets the texture name associated with this instance, if any.
    /// </summary>
    internal string? TextureName => this.assetName?.BaseName;

    /// <summary>
    /// Gets the held texture if it's not disposed.
    /// </summary>
    /// <returns>Texture (or null if not possible).</returns>
    internal Texture2D? Get()
    {
        if ((this.dirty || this.texture.IsDisposed)
            && !string.IsNullOrWhiteSpace(this.assetName?.BaseName))
        {
            this.texture = Game1.content.Load<Texture2D>(this.assetName.BaseName);
            this.dirty = false;
        }
        return this.texture.IsDisposed ? null : this.texture;
    }

    /// <summary>
    /// Reloads the held texture if an asset name was provided.
    /// </summary>
    /// <returns>true if reloaded, false otherwise.</returns>
    internal bool Refresh()
    {
        if (!string.IsNullOrWhiteSpace(this.assetName?.BaseName))
        {
            this.texture = Game1.content.Load<Texture2D>(this.assetName.BaseName);
            this.dirty = false;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Marks the asset as dirty to be refreshed next time its needed.
    /// </summary>
    internal void MarkDirty() => this.dirty = true;
}
