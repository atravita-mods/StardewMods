using AtraShared.Integrations.GMCMAttributes;

namespace GiantCropFertilizer;

/// <summary>
/// The config class for this mod.
/// </summary>
internal sealed class ModConfig
{
    private double giantCropChance = 1.1d;

    /// <summary>
    /// Gets or sets the probability of a fertilized square producing a giant crop.
    /// </summary>
    [GMCMRange(0, 1.1)]
    public double GiantCropChance
    {
        get => this.giantCropChance;
        set => this.giantCropChance = Math.Clamp(value, 0, 1.1d);
    }

    /// <summary>
    /// Gets or sets a value indicating whether giant crops should be allowed off-farm.
    /// </summary>
    public bool AllowGiantCropsOffFarm { get; set; } = false;
}