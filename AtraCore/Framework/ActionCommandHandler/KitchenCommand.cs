using Microsoft.Xna.Framework;

using Netcode;

using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
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

        Chest? fridge = loc switch
        {
            FarmHouse house => house.fridge.Value,
            IslandFarmHouse islandHouse => islandHouse.fridge.Value,
            _ => null,
        };

        ActivateKitchenCopy(loc, fridge);
        return true;
    }

    // derived from GameLocation.ActivateKitchen
    private static void ActivateKitchenCopy(GameLocation loc, Chest? fridge)
    {
        if (fridge?.mutex.IsLocked() == true)
        {
            Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Kitchen_InUse"));
            return;
        }

        List<NetMutex> muticies = new();
        List<Chest> fridges = new();

        if (fridge is not null)
        {
            muticies.Add(fridge.mutex);
            fridges.Add(fridge);
        }

        foreach (SObject? item in loc.objects.Values)
        {
            if (item is Chest mini && ((mini.ParentSheetIndex == 216 && mini.bigCraftable.Value) || mini.fridge.Value))
            {
                muticies.Add(mini.mutex);
                fridges.Add(mini);
            }
        }

        MultipleMutexRequest? multiple_mutex_request = null;
        multiple_mutex_request = new(muticies, delegate
        {
            int width = 800 + (IClickableMenu.borderWidth * 2);
            int height = 600 + (IClickableMenu.borderWidth * 2);
            Vector2 topLeftPositionForCenteringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(width, height);
            Game1.activeClickableMenu = new CraftingPage(
                (int)topLeftPositionForCenteringOnScreen.X,
                (int)topLeftPositionForCenteringOnScreen.Y,
                width,
                height,
                cooking: true,
                standalone_menu: true,
                fridges)
            {
                exitFunction = () => multiple_mutex_request!.ReleaseLocks(),
            };
        }, delegate
        {
            Game1.showRedMessage(Game1.content.LoadString("Strings\\UI:Kitchen_InUse"));
        });
    }
}
