using AtraShared.Integrations.GMCMAttributes;

namespace LastDayToPlantRedux.Framework;

internal sealed class ModConfig
{
    public CropOptions CropsToDisplay { get; set; } = CropOptions.Purchaseable;

    public FertilizerOptions FertilizersToDisplay { get; set; } = FertilizerOptions.Seen;

    /// <summary>
    /// Gets or sets a list of crops (by name) that should always be included.
    /// </summary>
    [GMCMDefaultIgnore]
    public List<string> AllowSeedsList { get; set; } = new();

    [GMCMDefaultIgnore]
    public List<string> AllFertilizersList { get; set; } = new();
}

public enum CropOptions
{
    All,
    Purchaseable,
    Seen,
}

public enum FertilizerOptions
{
    All,
    Seen,
}