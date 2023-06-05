using StardewValley.Objects;

namespace StopRugRemoval.Framework.Menus.MiniFarmerMenu;

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
    internal BootsSlot(int x, int y, string name, Func<Boots?> getItem, Action<Boots?> setItem)
        : base(InventorySlotType.Boots, x, y, name, getItem, setItem)
    {
    }
}
