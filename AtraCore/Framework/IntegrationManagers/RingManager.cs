using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using CommunityToolkit.Diagnostics;

namespace AtraCore.Framework.IntegrationManagers;

/// <summary>
/// A class to centralize the logic for counting the number of rings a players has.
/// </summary>
public sealed class RingManager
{
    // APIs
    private static IWearMoreRingsAPI? wearMoreRings;

    // helpers
    private IMonitor monitor;

    public RingManager(IMonitor monitor, ITranslationHelper translation, IModRegistry registry)
    {
        this.monitor = monitor;

        if (wearMoreRings is null)
        {
            IntegrationHelper helper = new(monitor, translation, registry);
            _ = helper.TryGetAPI("bcmpinc.WearMoreRings", "5.1.0", out wearMoreRings);
        }
    }

    /// <summary>
    /// Gets whether or not a farmer is wearing a specific ring.
    /// </summary>
    /// <param name="farmer">Farmer in question.</param>
    /// <param name="which">Which ring id.</param>
    /// <returns>True if the farmer has that ring equipped, false otherwise.</returns>
    public bool IsFarmerWearingRing(Farmer farmer, int which)
    {
        Guard.IsNotNull(farmer, nameof(farmer));

        return wearMoreRings is null
            ? farmer.isWearingRing(which)
            : wearMoreRings.CountEquippedRings(farmer, which) > 0;
    }

    /// <summary>
    /// Counts the total number of a specific ring a farmer has equipped.
    /// </summary>
    /// <param name="farmer">Farmer in quesiton.</param>
    /// <param name="which">Which ring id.</param>
    /// <returns>Count of rings.</returns>
    public int CountRingsEquipped(Farmer farmer, int which)
    {
        Guard.IsNotNull(farmer, nameof(farmer));

        return wearMoreRings is null
            ? (farmer.leftRing.Value?.GetEffectsOfRingMultiplier(which) ?? 0) + (farmer.rightRing.Value?.GetEffectsOfRingMultiplier(which) ?? 0)
            : wearMoreRings.CountEquippedRings(farmer, which);
    }
}
