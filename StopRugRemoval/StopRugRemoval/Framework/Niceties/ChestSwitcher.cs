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

        if (!ModEntry.Config.ChestSwap || Game1.currentLocation is not { } location)
        {
            return false;
        }

        if (Game1.player?.ActiveObject is not { } active)
        {
            return false;
        }

        Vector2 tile = e.Cursor.GrabTile;
        if (!location.Objects.TryGetValue(tile, out SObject? obj) || obj.QualifiedItemId == active.QualifiedItemId)
        {
            return false;
        }

        if (!active.HasTypeBigCraftable() || !active.isPlaceable()
            || active.IsTapper() || active.HasContextTag("sign_item") || active.HasContextTag("torch_item")
            || active.QualifiedItemId is "(BC)62" or "(BC)163" or "(BC)208" or "(BC)209" or "(BC)211" or "(BC)214" or "(BC)248" or "(BC)238" // indoor pot or cask or workbench or minijukebox or woodchipper or phone
            || DataLoader.Machines(Game1.content).ContainsKey(active.QualifiedItemId))
        {
            return false;
        }

        Chest? chest = (obj as Chest) ?? (obj.heldObject.Value as Chest);
        if (chest is null || !chest.playerChest.Value)
        {
            return false;
        }

        // If someone else has the lock, bail.
        // This isn't perfect - someone can grab the lock in the short period between this and when I actually do things.
        // but it's the best I can do.
        NetMutex m = chest.GetMutex();
        m.Update(location);
        if (m.IsLocked() && !m.IsLockHeld())
        {
            return false;
        }

        IInventory inventory = chest.GetItemsForPlayer();

        if (!location.Objects.Remove(tile))
        {
            return false;
        }

        SObject? originalHeldItem = ReferenceEquals(chest, obj) ? obj.heldObject.Value : null;

        NetMutex? mutex = (chest.SpecialChestType == Chest.SpecialChestTypes.JunimoChest || chest.GlobalInventoryId is not null) ? chest.GetMutex() : null;

        if (mutex is not null)
        {
            mutex.RequestLock(
                () => MoveChestImpl(chest, tile, inventory, location, active, obj, originalHeldItem),
                () =>
                {
                    chest.shakeTimer = 50;
                    location.Objects[tile] = chest;
                });
        }
        else
        {
            MoveChestImpl(chest, tile, inventory, location, active, obj, originalHeldItem);
        }

        input.Suppress(e.Button);
        return true;
    }

    private static void MoveChestImpl(Chest chest, Vector2 tile, IInventory inventory, GameLocation location, SObject active, SObject original, SObject? originalHeldItem = null)
    {
        try
        {
            if (active.placementAction(location, (int)tile.X * Game1.tileSize, (int)tile.Y * Game1.tileSize, Game1.player)
                && location.Objects.TryGetValue(tile, out SObject? other) && (other as Chest ?? other.heldObject.Value as Chest) is Chest newChest
                && newChest.SpecialChestType != Chest.SpecialChestTypes.MiniShippingBin
                && (newChest.GetActualCapacity() - newChest.GetItemsForPlayer().Count) >= inventory.Count)
            {
                // copy color
                newChest.playerChoiceColor.Value = chest.playerChoiceColor.Value;

                // copy inventory
                foreach (Item? item in inventory)
                {
                    newChest.addItem(item);
                }

                chest.GetItemsForPlayer().Clear();
                chest.clearNulls();

                if (originalHeldItem is not null)
                {
                    if (newChest.heldObject.Value is not null)
                    {
                        newChest.addItem(originalHeldItem);
                    }
                    else
                    {
                        newChest.heldObject.Value = originalHeldItem;
                    }
                }

                // decrease active object.
                chest.performRemoveAction();
                Game1.player.reduceActiveItemByOne();
                Item inventoryChest = original.getOne();
                if (!Game1.player.addItemToInventoryBool(inventoryChest, true))
                {
                    location.debris.Add(new(inventoryChest, Game1.player.Position));
                }
            }
            else
            {
                // huh, something went weird, replace the chest.
                location.Objects[tile] = original;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("swapping chests", ex);
            location.Objects[tile] = original;
        }
    }
}
