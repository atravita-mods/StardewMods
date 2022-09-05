using AtraShared.Integrations.GMCMAttributes;

namespace LastDayToPlantRedux.Framework;

internal sealed class ModConfig
{
    public CropOptions CropsToDisplay { get; set; } = CropOptions.Purchaseable;

    /// <summary>
    /// Gets or sets a list of crops (by name) that should always be included.
    /// </summary>
    [GMCMDefaultIgnore]
    public List<string> AllowList { get; set; } = new();
}

public enum CropOptions
{
    All,
    Purchaseable,
    Seen
}
