using Netcode;

namespace LastDayToPlantRedux.Framework;

/// <summary>
/// Used to watch events on farmer.
/// Make sure that the only references to it is on farmer.
/// </summary>
internal class FarmerWatcher
{
    internal void Professions_OnArrayReplaced(NetList<int, NetInt> list, IList<int> before, IList<int> after)
    {
        if (!Context.IsMultiplayer
                && (before.Contains(Farmer.agriculturist) != after.Contains(Farmer.agriculturist)
                || before.Contains(Farmer.agriculturist + 100) != after.Contains(Farmer.agriculturist + 100)))
        {
            CropAndFertilizerManager.RequestReset();
        }
    }

    internal void Professions_OnElementChanged(NetList<int, NetInt> list, int index, int oldValue, int newValue)
    {
        if (!Context.IsMultiplayer
            && (oldValue == Farmer.agriculturist || newValue == Farmer.agriculturist || oldValue == Farmer.agriculturist + 100 || newValue == Farmer.agriculturist + 11))
        {
            CropAndFertilizerManager.RequestReset();
        }
    }

}
