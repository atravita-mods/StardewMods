using AtraShared.Utils.Extensions;

using NetEscapades.EnumGenerators;

namespace DresserMiniMenu.Framework.Menus.MiniFarmerMenuButtons;

/// <summary>
/// The type of item this is. Used for drawing the menu background.
/// </summary>
[EnumExtensions]
internal enum InventorySlotType
{
    /// <summary>
    /// A <see cref="StardewValley.Objects.Hat"/>
    /// </summary>
    Hat = 42,

    /// <summary>
    /// A <see cref="StardewValley.Objects.Ring"/>
    /// </summary>
    Ring = 41,

    /// <summary>
    /// A pair of <see cref="StardewValley.Objects.Boots"/>
    /// </summary>
    Boots = 40,

    /// <summary>
    /// Pants, <seealso cref="StardewValley.Objects.Clothing"/>
    /// </summary>
    Pants = 68,

    /// <summary>
    /// A shirt, <seealso cref="StardewValley.Objects.Clothing"/>
    /// </summary>
    Shirt = 69,
}

/// <summary>
/// Extension methods for <see cref="InventorySlotType"/>.
/// </summary>
internal static partial class InventorySlotTypeExtensions
{
    /// <summary>
    /// Plays the sound cue associated with this inventory type.
    /// </summary>
    /// <param name="type">Inventory slot type.</param>
    internal static void PlayEquipSound(this InventorySlotType type)
    {
        string cue = type switch
        {
            InventorySlotType.Hat => "grassyStep",
            InventorySlotType.Ring => "crit",
            _ => "sandyStep",
        };

        try
        {
            Game1.playSound(cue);
            if (type == InventorySlotType.Boots)
            {
                DelayedAction.playSoundAfterDelay("sandyStep", 150);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("playing equip sound", ex);
        }
    }
}