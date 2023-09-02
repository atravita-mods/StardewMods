using StardewValley.Objects;

namespace DresserMiniMenu.Framework.MiniFarmerMenuIcons;

/// <summary>
/// An inventory slot corresponding to boots.
/// </summary>
internal sealed class BootsSlot : InventorySlot<Boots>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BootsSlot"/> class.
    /// </summary>
    /// <param name="x">x location.</param>
    /// <param name="y">y location.</param>
    /// <param name="name">Name of the slot.</param>
    /// <param name="getItem">Function to get the item.</param>
    /// <param name="setItem">Function to set the item.</param>
    /// <param name="isActive">Whether or not this is active.</param>
    internal BootsSlot(int x, int y, string name, Func<Boots?> getItem, Action<Boots?> setItem, bool isActive = true)
        : base(InventorySlotType.Boots, x, y, name, getItem, setItem, isActive)
    {
    }

    /// <inheritdoc />
    public override bool AssignItem(Item? item, out Item? prev, bool playSound)
    {
        if (base.AssignItem(item, out prev, playSound))
        {
            (prev as Boots)?.onUnequip(Game1.player);
            (item as Boots)?.onEquip(Game1.player);
            return true;
        }
        return false;
    }
}
