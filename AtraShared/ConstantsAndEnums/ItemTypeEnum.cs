namespace AtraShared.ConstantsAndEnums;

using AtraShared.Utils;

using Microsoft.Xna.Framework;

using StardewValley.GameData.Objects;
using StardewValley.Objects;
using StardewValley.Tools;

/// <summary>
/// An enum that represents the various types of objects in stardew.
/// </summary>
[Flags]
public enum ItemTypeEnum : uint
{
    /// <summary>
    /// An item type that I don't know about.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A big craftable - <see cref="Game1.bigCraftableData"/>
    /// Use the Vector2 constructor.
    /// </summary>
    BigCraftable = 0b1 << 1,

    /// <summary>
    /// Boots - <see cref="StardewValley.Objects.Boots"/>
    /// </summary>
    Boots = 0b1 << 4 | SObject,

    /// <summary>
    /// Shirts - <see cref="StardewValley.Objects.Clothing" />
    /// </summary>
    Shirts = 0b1 << 5,

    /// <summary>
    /// Pants - <see cref="StardewValley.Objects.Clothing" />
    /// </summary>
    Pants = 0b1 << 12,

    /// <summary>
    /// Obsolete and used to support old data, the combination of shirts and pants.
    /// </summary>
    [Obsolete("Shirts and pants used to be combined.")]
    Clothing = Shirts | Pants,

    /// <summary>
    /// A colored SObject - <see cref="StardewValley.Objects.ColoredObject" />
    /// </summary>
    ColoredSObject = 0b1 << 3 | SObject,

    /// <summary>
    /// A furniture item. <see cref="StardewValley.Objects.Furniture"/>
    /// </summary>
    /// <remarks>NOTICE: Don't try to use the usual constructor here.
    /// <see cref="Furniture.GetFurnitureInstance(string, Vector2?) "/>
    /// will create the correct item.</remarks>
    Furniture = 0b1 << 7,

    /// <summary>
    /// A hat item. <see cref="StardewValley.Objects.Hat"/>
    /// </summary>
    Hat = 0b1 << 6,

    /// <summary>
    /// A ring item. <see cref="StardewValley.Objects.Ring" />
    /// </summary>
    /// <remarks>NOTICE: Rings must be passed through the ring constructor, else they won't work.</remarks>
    Ring = 0b1 << 2 | SObject,

    /// <summary>
    /// Any normal object. <see cref="Game1.objectData" />
    /// </summary>
    /// <remarks>NOTICE: this includes <see cref="Ring"/>, must handle rings carefully!</remarks>
    SObject = 0b1,

    /// <summary>
    /// Any tool. <see cref="StardewValley.Tool"/>, excluding MeleeWeapons.
    /// </summary>
    Tool = 0b1 << 10,

    /// <summary>
    /// Any wallpaper <see cref="StardewValley.Objects.Wallpaper"/>
    /// </summary>
    Wallpaper = 0b1 << 8,

    /// <summary>
    /// Any flooring item <see cref="StardewValley.Objects.Wallpaper"/>
    /// </summary>
    Flooring = 0b1 << 9,

    /// <summary>
    /// Any member of the class <see cref="StardewValley.Tools.MeleeWeapon"/>
    /// </summary>
    Weapon = 0b1 << 11,

    /// <summary>
    /// Any item that should actually be the recipe form.
    /// See <see cref="Item.IsRecipe"/>
    /// </summary>
    Recipe = 0b1 << 14,
}

/// <summary>
/// Extensions for the ItemType Enum.
/// </summary>
public static class ItemTypeEnumExtensions
{
    /// <summary>
    /// Tries to get the ItemTypeEnum for a specific item.
    /// </summary>
    /// <param name="item">Item to check.</param>
    /// <returns>The ItemTypeEnum.</returns>
    public static ItemTypeEnum GetItemType(this Item item)
    {
        ItemTypeEnum ret;
        switch (item)
        {
            case Boots:
                ret = ItemTypeEnum.Boots;
                break;
            case Clothing clothing:
                ret = clothing.clothesType.Value == Clothing.ClothesType.SHIRT ? ItemTypeEnum.Shirts : ItemTypeEnum.Pants;
                break;
            case Hat:
                ret = ItemTypeEnum.Hat;
                break;
            case Ring:
                ret = ItemTypeEnum.Ring;
                break;
            case ColoredObject:
                ret = ItemTypeEnum.ColoredSObject;
                break;
            case MeleeWeapon:
                ret = ItemTypeEnum.Weapon;
                break;
            case Tool:
                ret = ItemTypeEnum.Tool;
                break;
            case Wallpaper wallpaper:
                ret = wallpaper.isFloor.Value ? ItemTypeEnum.Flooring : ItemTypeEnum.Wallpaper;
                break;
            case Furniture:
                ret = ItemTypeEnum.Furniture;
                break;
            case SObject obj:
            {
                ret = obj.bigCraftable.Value ? ItemTypeEnum.BigCraftable : ItemTypeEnum.SObject;
                if (obj.IsRecipe)
                {
                    ret |= ItemTypeEnum.Recipe;
                }
                break;
            }
            default:
                return ItemTypeEnum.Unknown;
        }

        return ret;
    }

    public static string? GetQualifiedId(ItemTypeEnum type, string id)
    {
        type &= ~ItemTypeEnum.Recipe;
        if (type == ItemTypeEnum.ColoredSObject)
        {
            type = ItemTypeEnum.SObject;
        }

#pragma warning disable CS0618 // Type or member is obsolete - special handling for obsolete former member.
        if (type == ItemTypeEnum.Clothing)
        {
            return GetQualifiedId(ItemTypeEnum.Pants, id) ?? GetQualifiedId(ItemTypeEnum.Shirts, id);
        }
#pragma warning restore CS0618 // Type or member is obsolete

        return type switch
        {
            ItemTypeEnum.BigCraftable => ItemRegistry.GetTypeDefinition(ItemRegistry.type_bigCraftable)?.GetData(id).QualifiedItemId,
            ItemTypeEnum.Boots => ItemRegistry.GetTypeDefinition(ItemRegistry.type_boots)?.GetData(id).QualifiedItemId,
            ItemTypeEnum.Shirts => ItemRegistry.GetTypeDefinition(ItemRegistry.type_shirt)?.GetData(id).QualifiedItemId,
            ItemTypeEnum.Pants => ItemRegistry.GetTypeDefinition(ItemRegistry.type_pants)?.GetData(id).QualifiedItemId,
            ItemTypeEnum.Furniture => ItemRegistry.GetTypeDefinition(ItemRegistry.type_furniture)?.GetData(id).QualifiedItemId,
            ItemTypeEnum.Hat => ItemRegistry.GetTypeDefinition(ItemRegistry.type_hat)?.GetData(id).QualifiedItemId,
            ItemTypeEnum.Ring => Game1.objectData.TryGetValue(id, out ObjectData? data) && !ItemHelperUtils.RingFilter(id, data) ? $"{ItemRegistry.type_object}{id}" : null,
            ItemTypeEnum.SObject => ItemRegistry.GetTypeDefinition(ItemRegistry.type_object)?.GetData(id).QualifiedItemId,
            ItemTypeEnum.Tool => ItemRegistry.GetTypeDefinition(ItemRegistry.type_tool)?.GetData(id).QualifiedItemId,
            ItemTypeEnum.Weapon => ItemRegistry.GetTypeDefinition(ItemRegistry.type_weapon)?.GetData(id).QualifiedItemId,
            ItemTypeEnum.Wallpaper => ItemRegistry.GetTypeDefinition(ItemRegistry.type_wallpaper)?.GetData(id).QualifiedItemId,
            ItemTypeEnum.Flooring => ItemRegistry.GetTypeDefinition(ItemRegistry.type_floorpaper)?.GetData(id).QualifiedItemId,
            _ => null,
        };
    }
}