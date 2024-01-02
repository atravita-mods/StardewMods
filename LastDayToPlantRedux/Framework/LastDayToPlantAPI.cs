using AtraShared.ConstantsAndEnums;

namespace LastDayToPlantRedux.Framework;

/// <summary>
/// The API instance for this mod.
/// </summary>
public sealed class LastDayToPlantAPI : ILastDayToPlantAPI
{
    /// <inheritdoc />
    public int? GetDays(Profession profession, string fertilizer, string crop, StardewSeasons season)
        => CropAndFertilizerManager.GetDays(profession, fertilizer, crop, season);

    /// <inheritdoc />
    public IReadOnlyDictionary<string, int>? GetAll(Profession profession, string fertilizer, StardewSeasons season)
        => CropAndFertilizerManager.GetAll(profession, fertilizer, season);

    /// <inheritdoc />
    public KeyValuePair<KeyValuePair<Profession, string?>, int>[]? GetConditionsPerCrop(string crop, StardewSeasons season)
        => CropAndFertilizerManager.GetConditionsPerCrop(crop, season);

    /// <inheritdoc />
    public string[]? GetTrackedCrops()
        => CropAndFertilizerManager.GetTrackedCrops();
}
