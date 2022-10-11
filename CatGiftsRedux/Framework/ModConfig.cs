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
    public List<WeightedItem> UserDefinedItemList { get; set; } = new();

    private float minChance = 0f;

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

    private float maxChance = 0f;

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
    public List<string> ForageFromMaps { get; set; } = new() { "Forest", "Beach" };

    /// <summary>
    /// Gets or sets a value indicating whether whether or not the pet should be able to get items from animal products.
    /// </summary>
    public bool AnimalProductsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether whether or not the pet should be able to get products of an in-season crop.
    /// </summary>
    public bool SeasonalCropsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether whether or not the pet should be able to get the products of a crop growing on the farm.
    /// </summary>
    public bool OnFarmCropEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether whether or not the pet can bring you some seasonal tree fruit.
    /// </summary>
    public bool SeasonalFruit { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether whether or not the pet can bring you a ring.
    /// </summary>
    public bool RingsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether whether or not the pet can pick from the full items list.
    /// </summary>
    public bool AllItemsEnabled { get; set; } = true;

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
