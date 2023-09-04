using HarmonyLib;

using StardewValley.SpecialOrders.Objectives;

namespace SpecialOrdersExtended.Niceties;

/// <summary>
/// Handles patches against the ShipObjective.
/// </summary>
[HarmonyPatch(typeof(ShipObjective))]
internal static class ShipObjectivePatches
{
    /// <summary>
    /// Skip null items for OnShipped.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>true to continue to the rest of the function, false otherwise.</returns>
    [HarmonyPatch(nameof(ShipObjective.OnItemShipped))]
    private static bool Prefix(Item item)
        => item is not null;
}
