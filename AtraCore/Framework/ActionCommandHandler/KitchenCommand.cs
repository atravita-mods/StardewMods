using Netcode;

using StardewValley.Locations;
using StardewValley.Objects;

using xTile.Dimensions;

namespace AtraCore.Framework.ActionCommandHandler;

/// <summary>
/// Handles adding the kitchen command to every map.
/// </summary>
internal static class KitchenCommand
{
    /// <summary>
    /// Handles the kitchen command.
    /// </summary>
    /// <param name="loc">Game location.</param>
    /// <param name="parameters">Parameters.</param>
    /// <param name="who">The farmer.</param>
    /// <param name="location">The tile location.</param>
    /// <returns>True if handled, false otherwise.</returns>
    internal static bool ApplyKitchenCommand(GameLocation loc, ArraySegment<string> parameters, Farmer who, Location location)
    {
        if (!who.IsLocalPlayer)
        {
            return false;
        }

        NetRef<Chest>? fridge = loc switch
        {
            FarmHouse house => house.fridge,
            IslandFarmHouse islandHouse => islandHouse.fridge,
            _ => null,
        };

        loc.ActivateKitchen(fridge);
        return true;
    }
}
