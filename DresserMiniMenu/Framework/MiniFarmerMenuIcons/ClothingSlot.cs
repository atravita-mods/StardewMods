using AtraShared.ConstantsAndEnums;

using CommunityToolkit.Diagnostics;

using StardewValley.Objects;

namespace DresserMiniMenu.Framework.MiniFarmerMenuIcons;

/// <summary>
/// A slot for clothing.
/// </summary>
internal sealed class ClothingSlot : InventorySlot<Clothing>
{
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
    internal ClothingSlot(EquipmentType type, int x, int y, string name, Func<Clothing?> getItem, Action<Clothing?> setItem, bool isActive = true)
        : base(type, x, y, name, getItem, setItem, isActive)
    {
        if (type is not EquipmentType.Pants && type is not EquipmentType.Shirt)
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
            Clothing.ClothesType.PANTS => this.Type == EquipmentType.Pants,
            Clothing.ClothesType.SHIRT => this.Type == EquipmentType.Shirt,
            _ => false,
        };
    }
}
