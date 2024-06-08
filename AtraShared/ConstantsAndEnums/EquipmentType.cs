using NetEscapades.EnumGenerators;

namespace AtraShared.ConstantsAndEnums;

/// <summary>
/// The type of item this is. Used for drawing the menu background.
/// </summary>
[EnumExtensions]
public enum EquipmentType
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

    /// <summary>
    /// A trinket, <seealso cref="StardewValley.Objects.Trinket"/>
    /// </summary>
    Trinket = 70,
}

/// <summary>
/// Extension methods for <see cref="EquipmentType"/>.
/// </summary>
public static partial class EquipmentTypeExtensions
{
    /// <summary>
    /// Plays the sound cue associated with this inventory type.
    /// </summary>
    /// <param name="type">Inventory slot type.</param>
    public static void PlayEquipSound(this EquipmentType type)
    {
        string cue = type switch
        {
            EquipmentType.Hat => "grassyStep",
            EquipmentType.Ring => "crit",
            EquipmentType.Trinket => "clank",
            _ => "sandyStep",
        };

        try
        {
            Game1.playSound(cue);
            if (type == EquipmentType.Boots)
            {
                DelayedAction.playSoundAfterDelay("sandyStep", 150);
            }
        }
        catch (Exception ex)
        {
            AtraBase.Internal.Logger.Instance.Error("playing equip sound", ex);
        }
    }
}