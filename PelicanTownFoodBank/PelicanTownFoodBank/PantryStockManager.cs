using AtraBase.Toolkit.StringHandler;

using AtraShared.Utils;
using AtraShared.Wrappers;

using Microsoft.Xna.Framework;
using PelicanTownFoodBank.Models;
using StardewModdingAPI.Utilities;

namespace PelicanTownFoodBank;

/// <summary>
/// Static class that manages the shop stock for the food pantry.
/// </summary>
internal static class PantryStockManager
{
    private static readonly PerScreen<Lazy<List<int>>> PerScreenedSellables = new(() => new Lazy<List<string>>(SetUpInventory));
    private static readonly PerScreen<HashSet<ISalable>> PerScreenedBuyBacks = new(() => new HashSet<ISalable>());

    /// <summary>
    /// Gets a list of integers that corresponds to the shop's stock.
    /// </summary>
    internal static List<int> Sellables => PerScreenedSellables.Value.Value;

    /// <summary>
    /// Gets a Dictionary consisting of the sold back objects and their quantities.
    /// </summary>
    internal static HashSet<ISalable> BuyBacks => PerScreenedBuyBacks.Value;

    /// <summary>
    /// Gets the categories of SObject the food bank deals with...
    /// </summary>
    internal static int[] FoodBankCategories { get; } =
    [
            SObject.artisanGoodsCategory,
            SObject.CookingCategory,
            SObject.EggCategory,
            SObject.FishCategory,
            SObject.FruitsCategory,
            SObject.GreensCategory,
            SObject.ingredientsCategory,
            SObject.meatCategory,
            SObject.MilkCategory,
            SObject.sellAtFishShopCategory,
            SObject.sellAtPierres,
            SObject.sellAtPierresAndMarnies,
            SObject.syrupCategory,
            SObject.VegetableCategory,
    ];

    /// <summary>
    /// Gets the current food pantry menu.
    /// </summary>
    /// <returns>Food pantry menu.</returns>
    internal static FoodBankMenu GetFoodBankMenu()
    {
        Dictionary<ISalable, int[]> sellables = Sellables.ToDictionary((int i) => (ISalable)new SObject(Vector2.Zero, i, 1), (_) => new int[] { 0, 1 });
        foreach (ISalable buyback in BuyBacks)
        {
            sellables[buyback] = [0, buyback.Stack];
        }
        return new(sellables, BuyBacks);
    }

    /// <summary>
    /// Resets data structures. Call **per** player.
    /// </summary>
    internal static void Reset()
    {
        BuyBacks.Clear();
        PerScreenedSellables.Value = new Lazy<List<string>>(SetUpInventory);
    }

    /// <summary>
    /// Sets up the daily inventory.
    /// </summary>
    /// <returns>The daily inventory.</returns>
    internal static List<string> SetUpInventory()
    {
        Random seededRandom = RandomUtils.GetSeededRandom(6, "atravita.CCOverhaul");
        List<int> neededIngredients = GetNeededIngredients();
        (List<string> cookingIngredients, List<string> cookedItems) = GetOtherSellables();
        Utility.Shuffle(seededRandom, neededIngredients);
        Utility.Shuffle(seededRandom, cookingIngredients);
        Utility.Shuffle(seededRandom, cookedItems);

        List<int> sellables = new(neededIngredients.Take(5));
        sellables.AddRange(cookingIngredients.Take(5));
        sellables.AddRange(cookedItems.Take(3));
        Utility.Shuffle(seededRandom, sellables);
        return sellables;
    }

    private static List<int> GetNeededIngredients()
    {
        List<int> neededIngredients = new(24);
        Dictionary<string, string> recipes = DataLoader.CookingRecipes(Game1.content);
        foreach ((string learned_recipe, int number_made) in Game1.player.cookingRecipes.Pairs)
        {
            if (number_made != 0 )
            {
                continue;
            }
            else if (recipes.TryGetValue(learned_recipe, out string? recipe) && recipe.IndexOf('/') is int index && index > 0)
            {
                SpanSplit ingredients = recipe[..index].SpanSplit();
                for(int i = 0; i < ingredients.Count; i += 2)
                {
                    if(int.TryParse(ingredients[i], out int ingredient) && ingredient > 0)
                    {
                        neededIngredients.Add(ingredient);
                    }
                }
            }
        }
        return neededIngredients;
    }

    private static (List<string> cookingIngredients, List<string> cookedItems) GetOtherSellables()
    {
        List<string> cookingIngredients = new(24);
        List<string> cookedItems = new(24);
        foreach ((string? index, StardewValley.GameData.Objects.ObjectData? data) in Game1Wrappers.ObjectData)
        {
            int cat = data.Category;
            int price = data.Price;

            if (FoodBankCategories.Contains(cat) && price < 250)
            {
                if (cat == SObject.CookingCategory)
                {
                    cookedItems.Add(index);
                }
                else
                {
                    cookingIngredients.Add(index);
                }
            }
        }

        return (cookingIngredients, cookedItems);
    }
}