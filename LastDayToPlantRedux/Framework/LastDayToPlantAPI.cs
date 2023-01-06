using AtraShared.ConstantsAndEnums;

namespace LastDayToPlantRedux.Framework;

/// <summary>
/// The API instance for this mod.
/// </summary>
public sealed class LastDayToPlantAPI : ILastDayToPlantAPI
{
    /// <inheritdoc />
    public int? GetDays(Profession profession, int fertilizer, int crop, StardewSeasons season)
        => CropAndFertilizerManager.GetDays(profession, fertilizer, crop, season);

    /// <inheritdoc />
    public IReadOnlyDictionary<int, int>? GetAll(Profession profession, int fertilizer, StardewSeasons season)
        => CropAndFertilizerManager.GetAll(profession, fertilizer, season);

    /// <inheritdoc />
    public KeyValuePair<KeyValuePair<Profession, int>, int>[]? GetConditionsPerCrop(int crop, StardewSeasons season)
        => CropAndFertilizerManager.GetConditionsPerCrop(crop, season);

    /// <inheritdoc />
    public int[]? GetTrackedCrops()
        => CropAndFertilizerManager.GetTrackedCrops();
}
