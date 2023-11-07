namespace EastScarp.Models;

using Microsoft.Xna.Framework;

// while it might be tempting to use records here, note that records are immutable
// and thus play not very nicely with CP editing.
// use normal pocos.

/// <summary>
/// The data used for emoji overrides.
/// </summary>
public sealed class EmojiData
{
    /// <summary>
    /// Gets or sets the asset location of the emoji.
    /// </summary>
    public string AssetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the upper left pixel of the emoji. All emoji are 9x9.
    /// </summary>
    public Point Location { get; set; }

}
