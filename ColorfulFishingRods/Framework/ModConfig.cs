namespace ColorfulFishingRods.Framework;

using Microsoft.Xna.Framework;

/// <summary>
/// The config class for this mod.
/// </summary>
public sealed class ModConfig
{
    /// <summary>
    /// A map of colors and overrides.
    /// </summary>
    public Dictionary<string, Color> Map = new();
}
