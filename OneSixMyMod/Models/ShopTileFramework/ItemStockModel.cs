namespace OneSixMyMod.Models.ShopTileFramework;
public record ItemShopModel(
    STFItemType ItemType,
    bool IsRecipe,
    int StockPrice = -1,
    STFCurrency StockItemCurrency = STFCurrency.Money,
    int StockCurrencyStack = 1,
    int Quality = 0,
    int[]? ItemIDs = null,
    string[]? JAPacks = null,
    string[]? ExcludeFromJAPacks = null,
    string[]? ItemNames  = null,
    bool FilterSeedsBySeason = true,
    int Stock = int.MaxValue,
    int MaxNumItemsSoldInItemStock = int.MaxValue,
    string[]? When = null);

public enum STFItemType
{
    Object,
    Seed,
    BigCraftable,
    Clothing,
    Ring,
    Hat,
    Boot,
    Furniture,
    Weapon,
    Floors,
    Wallpaper
}

public enum STFCurrency
{
    Money,
    festivalScore,
    clubCoins
}