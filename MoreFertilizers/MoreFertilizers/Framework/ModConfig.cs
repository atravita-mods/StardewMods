using AtraShared.Integrations.GMCMAttributes;
using Microsoft.Xna.Framework;

namespace MoreFertilizers.Framework;

/// <summary>
/// The configuration class for this mod.
/// </summary>
internal sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether or not the mill should produce organic goods.
    /// </summary>
    public bool MillProducesOrganic { get; set; } = true;

    /// <summary>
    /// Gets or sets a value for what color to make the water overlay for fish food.
    /// </summary>
    [GMCMDefaultColor(147, 112, 219, 155)]
    public Color WaterOverlayColor { get; set; } = new(147, 112, 219, 155);

    /// <summary>
    /// Gets or sets a value indicating whether trees should be recolored when they're fertilized.
    /// </summary>
    public bool RecolorFruitTrees { get; set; } = true;
}