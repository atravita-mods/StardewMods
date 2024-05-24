using CommunityToolkit.Diagnostics;

using StardewValley.Objects;

namespace DresserMiniMenu.Framework.MiniFarmerMenuIcons;

/// <summary>
/// A slot for clothing.
/// </summary>
internal sealed class ClothingSlot : InventorySlot<Clothing>
{
    private const int SHIRT = 0;
    private const int PANTS = 1;
    private const int ACCESSORY = 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClothingSlot"/> class.
    /// </summary>
    /// <param name="type">The clothing type (shirt or pants).</param>
    /// <param name="x">The x pixel location.</param>
    /// <param name="y">The y pixel location.</param>
    /// <param name="name">The name, for debugging.</param>
    /// <param name="getItem">The function that gets the item.</param>
    /// <param name="setItem">The function that sets the item.</param>
    /// <param name="isActive">Whether or not this slot should be active.</param>
    internal ClothingSlot(InventorySlotType type, int x, int y, string name, Func<Clothing?> getItem, Action<Clothing?> setItem, bool isActive = true)
        : base(type, x, y, name, getItem, setItem, isActive)
    {
        if (type is not InventorySlotType.Pants && type is not InventorySlotType.Shirt)
        {
            ThrowHelper.ThrowArgumentException($"type must be InventorySlotType.Pants or InventorySlotType.Shirt");
        }
    }

    /// <inheritdoc />
    public override bool CanAcceptItem(Item? item)
    {
        if (!base.CanAcceptItem(item))
        {
            return false;
        }

        if (item is null)
        {
            return true;
        }

        return (item as Clothing)?.clothesType.Value switch
        {
            PANTS => this.Type == InventorySlotType.Pants,
            SHIRT => this.Type == InventorySlotType.Shirt,
            _ => false,
        };
    }
}
