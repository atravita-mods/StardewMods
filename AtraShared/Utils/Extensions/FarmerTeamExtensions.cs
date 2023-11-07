namespace AtraShared.Utils.Extensions;

using CommunityToolkit.Diagnostics;

using StardewValley.SpecialOrders;

/// <summary>
/// Extensions for FarmerTeam.
/// </summary>
public static class FarmerTeamExtensions
{
    /// <summary>
    /// Gets a value indicating of a special order is currently active or has been completed.
    /// </summary>
    /// <param name="farmerTeam">FarmerTeam to check.</param>
    /// <param name="special_order_key">Special order key to check for.</param>
    /// <returns>True if completed.</returns>
    public static bool SpecialOrderActiveOrCompleted(this FarmerTeam farmerTeam, string special_order_key)
    {
        Guard.IsNotNull(farmerTeam);
        Guard.IsNotNullOrEmpty(special_order_key);

        if (farmerTeam.completedSpecialOrders.Contains(special_order_key))
        {
            return true;
        }

        foreach (SpecialOrder? order in farmerTeam.specialOrders)
        {
            if (order.questKey.Value == special_order_key && order.questState.Value is SpecialOrderStatus.Complete or SpecialOrderStatus.InProgress)
            {
                return true;
            }
        }

        return false;
    }
}
