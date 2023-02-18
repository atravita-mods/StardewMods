using AtraShared.Integrations.GMCMAttributes;

using Microsoft.Xna.Framework;

namespace GrowableBushes.Framework;

/// <summary>
/// The config class for this mod.
/// </summary>
internal sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether or not players should be able to axe non-placed bushes.
    /// </summary>
    public bool CanAxeAllBushes { get; set; } = false;

    /// <summary>
    /// Gets or sets where the default shop location is.
    /// </summary>
    [GMCMDefaultVector(1, 7)]
    public Vector2 ShopLocation { get; set; } = new(1, 7);

    /// <summary>
    /// Gets or sets a value indicating whether or not the bush shop should have a little graphic.
    /// </summary>
    public bool ShowBushShopGraphic { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not npcs should be able to trample bushes in their way.
    /// </summary>
    public bool ShouldNPCsTrampleBushes { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to disable some placement rules.
    /// </summary>
    public bool RelaxedPlacement { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not mod data should be preserved.
    /// </summary>
    public bool PreserveModData { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not bushes should stack.
    /// </summary>
    public bool AllowBushStacking { get; set; } = true;
}
