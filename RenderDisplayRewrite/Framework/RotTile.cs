using Microsoft.Xna.Framework.Graphics;

using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace RenderDisplayRewrite.Framework;

/// <summary>
/// A tile that tracks its rotation.
/// </summary>
internal class RotTile : StaticTile
{
    /// <summary>
    /// The string key used for flips.
    /// </summary>
    internal const string FLIP = "@Flip";

    /// <summary>
    /// The string key used for rotation.
    /// </summary>
    internal const string ROT = "@Rotation";

    /// <inheritdoc cref="StaticTile.StaticTile(Layer, TileSheet, BlendMode, int)"/>
    public RotTile(Layer layer, TileSheet tileSheet, BlendMode blendMode, int tileIndex)
        : base(layer, tileSheet, blendMode, tileIndex)
    {
        this.UpdateEffects();
        this.UpdateRotation();
    }

    /// <summary>
    /// Gets the spriteeffects to use for this tile.
    /// </summary>
    internal SpriteEffects Effects { get; private set; }

    /// <summary>
    /// Gets the rotation to use for this tile.
    /// </summary>
    internal float Rotation { get; private set; }

    /// <inheritdoc/>
    public override Tile Clone(Layer layer)
    {
        RotTile newTile = new(layer, this.TileSheet, this.BlendMode, this.TileIndex)
        {
            Rotation = this.Rotation,
            Effects = this.Effects,
        };
        return newTile;
    }

    /// <summary>
    /// Parses the effects from a tile.
    /// </summary>
    /// <param name="tile">Tile to parse from.</param>
    /// <returns>SpriteEffects to use.</returns>
    internal static SpriteEffects ParseEffects(Tile tile)
        => tile.Properties.TryGetValue(FLIP, out PropertyValue? prop) && int.TryParse(prop, out int value)
        ? (SpriteEffects)value : SpriteEffects.None;

    /// <summary>
    /// Parses the rotation from a tile.
    /// </summary>
    /// <param name="tile">Tile to parse from.</param>
    /// <returns>Rotation in radians.</returns>
    internal static float ParseRotation(Tile tile)
    {
        if (tile.Properties.TryGetValue(ROT, out PropertyValue? prop) && int.TryParse(prop, out int value))
        {
            value %= 360;

            return value switch
            {
                0 => 0f,
                90 => MathF.PI / 2,
                180 => MathF.PI,
                270 => -MathF.PI / 2,
                _ => MathF.PI * (value / 2), // who hurt you?
            };
        }
        return 0f;
    }

    /// <summary>
    /// Updates the Effects used for this tile.
    /// </summary>
    internal void UpdateEffects() =>
        this.Effects = ParseEffects(this);

    /// <summary>
    /// Updates the rotation used for this tile.
    /// </summary>
    internal void UpdateRotation() => this.Rotation = ParseRotation(this);
}
