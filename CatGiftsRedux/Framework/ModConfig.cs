using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations.GMCMAttributes;

namespace CatGiftsRedux.Framework;


[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "This is a record.")]
public record ItemRecord(ItemTypeEnum Type, string Identifier);

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Stylecop doesn't understand records.")]
public record WeightedItem(ItemRecord Item, double Weight);

/// <summary>
/// The config class for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Fields kept near accessors.")]
public sealed class ModConfig
{
    /// <summary>
    /// Gets or sets the list of items never to produce (unless it's in the user list.)
    /// </summary>
    [GMCMDefaultIgnore]
    public HashSet<ItemRecord> Denylist { get; set; } = new()
    {
        new (ItemTypeEnum.SObject, "Mango Sapling"),
        new (ItemTypeEnum.SObject, "Banana Sapling"),
        new (ItemTypeEnum.SObject, "Mango"),
        new (ItemTypeEnum.SObject, "Banana"),
    };

    /// <summary>
    /// Gets or sets a list of things the user wants to drop.
    /// </summary>
    [GMCMDefaultIgnore]
    public List<WeightedItem> UserDefinedItemList { get; set; } = new()
    {
        new (
            new (ItemTypeEnum.SObject, "Trash"),
            100),
        new (
            new (ItemTypeEnum.SObject, "Stick"),
            100),
    };

    /// <summary>
    /// Gets or sets how much to weigh the user defined list.
    /// </summary>
    [GMCMRange(0, 1000)]
    public int UserDefinedListWeight { get; set; } = 100;

    private float minChance = 0.1f;

    /// <summary>
    /// Gets or sets chance the pet will bring you something at minimum hearts.
    /// </summary>
    [GMCMInterval(0.01)]
    [GMCMRange(0, 1.0)]
    public float MinChance
    {
        get => this.minChance;
        set => this.minChance = Math.Clamp(value, 0f, 1.0f);
    }

    private float maxChance = 0.7f;

    /// <summary>
    /// Gets or sets the chance the pet will bring you something at max hearts.
    /// </summary>
    [GMCMInterval(0.01)]
    [GMCMRange(0, 1.0)]
    public float MaxChance
    {
        get => this.maxChance;
        set => this.maxChance = Math.Clamp(value, 0f, 1.0f);
    }

    private int weeklyLimit = 3;

    /// <summary>
    /// Gets or sets the maximum number of items the pet will bring you in a week.
    /// </summary>
    [GMCMRange(0, 7)]
    public int WeeklyLimit
    {
        get => this.weeklyLimit;
        set => this.weeklyLimit = Math.Clamp(value, 0, 7);
    }

    /// <summary>
    /// Gets or sets a list of place names pets can bring you forage from.
    /// </summary>
    [GMCMDefaultIgnore]
    public List<string> ForageFromMaps { get; set; } = new() { "Forest", "Beach", "Mountain" };

    /// <summary>
    /// Gets or sets a value indicating how much to weigh the forage from maps picker.
    /// </summary>
    [GMCMRange(0, 1000)]
    public int ForageFromMapsWeight { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating the weight of the animal products picker.
    /// </summary>
    [GMCMRange(0, 1000)]
    public int AnimalProductsWeight { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating the weight of the seasonal crops picker.
    /// </summary>
    [GMCMRange(0, 1000)]
    public int SeasonalCropsWeight { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating the weight of the on farm crops picker.
    /// </summary>
    [GMCMRange(0, 1000)]
    public int OnFarmCropWeight { get; set; } = 50;

    /// <summary>
    /// Gets or sets a value indicating the weight of the seasonal fruit picker.
    /// </summary>
    [GMCMRange(0, 1000)]
    public int SeasonalFruitWeight { get; set; } = 50;

    /// <summary>
    /// Gets or sets a value indicating the chances the pet will bring you a ring.
    /// </summary>
    [GMCMRange(0, 1000)]
    public int RingsWeight { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating the weight of the Daily Saloon dish.
    /// </summary>
    [GMCMRange(0, 1000)]
    public int DailyDishWeight { get; set; } = 10;

    [GMCMRange(0, 1000)]
    public int HatWeight { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether or not the pet can pick from the full items list.
    /// </summary>
    [GMCMRange(0, 1000)]
    public int AllItemsWeight { get; set; } = 100;

    private int maxPriceForAllItems = 500;

    /// <summary>
    /// Gets or sets the most valuable item the pet can bring you.
    /// </summary>
    [GMCMRange(0, 1000)]
    public int MaxPriceForAllItems
    {
        get => this.maxPriceForAllItems;
        set => this.maxPriceForAllItems = Math.Clamp(value, 0, 1000);
    }
}
