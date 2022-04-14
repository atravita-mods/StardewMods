namespace GiantCropFertilizer;

/// <summary>
/// The config class for this mod.
/// </summary>
public class ModConfig
{
    private double giantCropChance = 1.1d;

    /// <summary>
    /// Gets or sets the probability of a fertilized square producing a giant crop.
    /// </summary>
    public double GiantCropChance
    {
        get => this.giantCropChance;
        set => this.giantCropChance = Math.Clamp(value, 0, 1.1d);
    }
}