using AtraShared.Integrations.GMCMAttributes;

using Microsoft.Xna.Framework;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// The configuration class for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Fields kept near accessors.")]
public sealed class ModConfig
{
    /// <summary>
    /// Gets or sets the tile of the giant crop shop's location.
    /// </summary>
    [GMCMDefaultVector(8, 14)]
    public Vector2 GiantCropShopLocation { get; set; } = new(8, 14);

    // TODO: check positioning with SVE.

    /// <summary>
    /// Gets or sets the tile of the resource shop's location.
    /// </summary>
    [GMCMDefaultVector(6, 19)]
    public Vector2 ResourceShopLocation { get; set; } = new(6, 19);

    /// <summary>
    /// Gets or sets a value indicating whether or not NPCs should trample placed resource clumps.
    /// </summary>
    public bool ShouldNPCsTrampleResourcesClumps { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not NPCs should be able to trample giant crops.
    /// </summary>
    public bool ShouldNPCsTrampleGiantCrops { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether relaxed placement rules should be allowed.
    /// </summary>
    public bool RelaxedPlacement { get; set; } = false;

    private int shovelEnergy = 7;

    /// <summary>
    /// Gets or sets how much energy the shovel uses.
    /// </summary>
    [GMCMRange(0, 25)]
    public int ShovelEnergy
    {
        get => this.shovelEnergy;
        set => this.shovelEnergy = Math.Clamp(this.shovelEnergy, 0, 25);
    }
}
