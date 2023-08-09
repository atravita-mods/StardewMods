using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Netcode;

using StardewValley.Locations;
using StardewValley.Objects;

using xTile.Dimensions;

namespace AtraCore.Framework.ActionCommandHandler;
internal static class KitchenCommand
{
    internal static bool ApplyKitchenCommand(GameLocation loc, ArraySegment<string> parameters, Farmer who, Location location)
    {
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
