namespace LastDayToPlantRedux.Framework;
public class LastDayToPlantAPI : ILastDayToPlantAPI
{
    public int? GetDays(Profession profession, int fertilizer, int crop)
        => CropAndFertilizerManager.GetDays(profession, fertilizer, crop);
}
