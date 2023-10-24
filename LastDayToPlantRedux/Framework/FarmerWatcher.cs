namespace LastDayToPlantRedux.Framework;

/// <summary>
/// Used to watch events on farmer.
/// Make sure that the only references to it is on farmer.
/// </summary>
internal class FarmerWatcher
{
    private const int PRESTIGED = Farmer.agriculturist + 100;

    internal void OnValueChanged(int value)
    {
        if (value == Farmer.agriculturist || value == PRESTIGED)
        {
            CropAndFertilizerManager.RequestReset();
        }
    }
}
