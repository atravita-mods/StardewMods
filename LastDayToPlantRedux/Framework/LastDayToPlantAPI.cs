namespace LastDayToPlantRedux.Framework;

/// <summary>
/// The API instance for this mod.
/// </summary>
public sealed class LastDayToPlantAPI : ILastDayToPlantAPI
{
    /// <inheritdoc />
    public int? GetDays(Profession profession, int fertilizer, int crop)
        => CropAndFertilizerManager.GetDays(profession, fertilizer, crop);

    /// <inheritdoc />
    public IReadOnlyDictionary<int, int>? GetAll(Profession profession, int fertilizer)
        => CropAndFertilizerManager.GetAll(profession, fertilizer);

    /// <inheritdoc />
    public KeyValuePair<KeyValuePair<Profession, int>, int>[]? GetConditionsPerCrop(int crop)
        => CropAndFertilizerManager.GetConditionsPerCrop(crop);

    /// <inheritdoc />
    public int[]? GetTrackedCrops()
        => CropAndFertilizerManager.GetTrackedCrops();
}
