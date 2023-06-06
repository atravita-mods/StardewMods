using AtraShared.Utils.Extensions;

using StardewValley.Objects;

namespace DresserMiniMenu.Framework.Menus.MiniFarmerMenu;

/// <summary>
/// An inventory slot that holds a ring.
/// </summary>
internal sealed class RingSlot : InventorySlot<Ring>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RingSlot"/> class.
    /// </summary>
    /// <param name="x">x location.</param>
    /// <param name="y">y location.</param>
    /// <param name="name">slot name.</param>
    /// <param name="getItem">function to get the ring.</param>
    /// <param name="setItem">function to set the ring.</param>
    /// <param name="isActive">Whether or not this slot is active.</param>
    internal RingSlot(int x, int y, string name, Func<Ring?> getItem, Action<Ring?> setItem, bool isActive = true)
        : base(InventorySlotType.Ring, x, y, name, getItem, setItem, isActive)
    {
    }

    /// <inheritdoc />
    public override bool AssignItem(Item? item, out Item? prev, bool playSound)
    {
        if (base.AssignItem(item, out prev, playSound))
        {
            try
            {
                (item as Ring)?.onEquip(Game1.player, Game1.currentLocation);
                (prev as Ring)?.onUnequip(Game1.player, Game1.currentLocation);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError($"equipping or dequipping rings ({prev?.Name ?? "null" }->{item?.Name ?? "null" })", ex);
            }
            return true;
        }

        return false;
    }
}
