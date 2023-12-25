using System;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

using StardewValley.Extensions;
using StardewValley.Inventories;
using StardewValley.Network;
using StardewValley.Objects;

namespace StopRugRemoval.Framework.Niceties;

/// <summary>
/// Switches out chests.
/// </summary>
internal static class ChestSwitcher
{
    /// <summary>
    /// Switches between chests.
    /// </summary>
    /// <param name="e">event args.</param>
    /// <param name="input">input handler.</param>
    /// <returns>True if handled, false otherwise.</returns>
    internal static bool Switch(ButtonPressedEventArgs e, IInputHelper input)
    {
        if (!e.Button.IsActionButton() && !e.Button.IsUseToolButton())
        {
            return false;
        }

        if (Game1.currentLocation is not { } location)
        {
            return false;
        }

        if (Game1.player?.ActiveObject is not { } active || !active.HasTypeBigCraftable() || !active.isPlaceable())
        {
            return false;
        }

        Microsoft.Xna.Framework.Vector2 tile = e.Cursor.GrabTile;
        if (!location.Objects.TryGetValue(tile, out SObject? obj) || obj is not Chest chest || chest.QualifiedItemId == active.QualifiedItemId)
        {
            return false;
        }

        StardewValley.Inventories.IInventory inventory = chest.GetItemsForPlayer();

        if (!location.Objects.Remove(tile))
        {
            return false;
        }

        if (chest.GlobalInventoryId is { } globalInventoryId)
        {
            NetMutex mutex = Game1.player.team.GetOrCreateGlobalInventoryMutex(globalInventoryId);
            mutex.RequestLock(
                () => MoveChestImpl(chest, tile, inventory, location, active),
                () =>
                {
                    ModEntry.ModMonitor.Log($"hit locked", LogLevel.Error);
                    chest.shakeTimer = 50;
                    location.Objects[tile] = chest;
                });
        }
        else
        {
            MoveChestImpl(chest, tile, inventory, location, active);
        }

        input.Suppress(e.Button);
        return true;
    }

    private static void MoveChestImpl(Chest chest, Vector2 tile, IInventory inventory, GameLocation location, SObject active)
    {
        try
        {
            location.Objects.TryGetValue(tile, out var what);

            if (active.placementAction(location, (int)tile.X * Game1.tileSize, (int)tile.Y * Game1.tileSize, Game1.player)
                && location.Objects.TryGetValue(tile, out SObject? other) && other is Chest newChest
                && newChest.SpecialChestType != Chest.SpecialChestTypes.MiniShippingBin
                && (newChest.GetActualCapacity() - newChest.GetItemsForPlayer().Count) >= inventory.Count)
            {
                foreach (Item? item in inventory)
                {
                    newChest.addItem(item);
                }

                chest.GetItemsForPlayer().Clear();
                chest.clearNulls();
                chest.performRemoveAction();
                Game1.player.reduceActiveItemByOne();
                Item inventoryChest = chest.getOne();
                if (!Game1.player.addItemToInventoryBool(inventoryChest, true))
                {
                    location.debris.Add(new(inventoryChest, Game1.player.Position));
                }
            }
            else
            {
                location.Objects[tile] = chest;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("swapping chests", ex);
            location.Objects[tile] = chest;
        }
    }
}
