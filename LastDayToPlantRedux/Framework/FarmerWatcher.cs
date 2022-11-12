using Netcode;

namespace LastDayToPlantRedux.Framework;

/// <summary>
/// Used to watch events on farmer.
/// Make sure that the only references to it is on farmer.
/// </summary>
internal class FarmerWatcher
{
    private const int prestiged = Farmer.agriculturist + 100;

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used to listen to an event.")]
    internal void Professions_OnArrayReplaced(NetList<int, NetInt> list, IList<int> before, IList<int> after)
    {
        if ( before.Contains(Farmer.agriculturist) != after.Contains(Farmer.agriculturist)
                || before.Contains(prestiged) != after.Contains(prestiged))
        {
            CropAndFertilizerManager.RequestReset();
        }
    }

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used to listen to an event.")]
    internal void Professions_OnElementChanged(NetList<int, NetInt> list, int index, int oldValue, int newValue)
    {
        if (oldValue == Farmer.agriculturist || newValue == Farmer.agriculturist || oldValue == prestiged || newValue == prestiged)
        {
            CropAndFertilizerManager.RequestReset();
        }
    }
}
