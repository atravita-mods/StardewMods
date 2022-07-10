using Microsoft.Xna.Framework;

namespace MuseumRewardsIn;

/// <summary>
/// The config class for this mod.
/// </summary>
internal sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating where to stick
    /// the box for the museum shop.
    /// </summary>
    public Vector2 BoxLocation { get; set; } = new(-1, -1);

}
