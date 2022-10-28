using AtraShared.Integrations.GMCMAttributes;

namespace LastDayToPlantRedux.Framework;

internal sealed class ModConfig
{
    private CropOptions cropsToDisplay = CropOptions.Purchaseable;
    private FertilizerOptions fertilizersToDisplay = FertilizerOptions.Seen;

    public CropOptions CropsToDisplay
    {
        get => this.cropsToDisplay;
        set
        {
            if (value != this.cropsToDisplay)
            {
                CropAndFertilizerManager.RequestInvalidateCrops();
            }
            this.cropsToDisplay = value;
        }
    }

    public FertilizerOptions FertilizersToDisplay
    {
        get => this.fertilizersToDisplay;
        set
        {
            if (value != this.fertilizersToDisplay)
            {
                CropAndFertilizerManager.RequestInvalidateFertilizers();
            }
            this.fertilizersToDisplay = value;
        }
    }

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