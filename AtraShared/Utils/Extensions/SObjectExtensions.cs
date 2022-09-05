using CommunityToolkit.Diagnostics;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extensions for SObject.
/// </summary>
public static class SObjectExtensions
{
    /// <summary>
    /// Gets whether or not an SObject is a trash item.
    /// </summary>
    /// <param name="obj">SObject to check.</param>
    /// <returns>true if it's a trash item, false otherwise.</returns>
    public static bool IsTrashItem(this SObject obj)
        => obj is not null && !obj.bigCraftable.Value && (obj.ParentSheetIndex >= 168 && obj.ParentSheetIndex < 173);

    /// <summary>
    /// Gets the public name of a bigcraftable.
    /// </summary>
    /// <param name="bigCraftableIndex">Bigcraftable.</param>
    /// <returns>public name if found.</returns>
    public static string GetBigCraftableName(this int bigCraftableIndex)
    {
        if (Game1.bigCraftablesInformation.TryGetValue(bigCraftableIndex, out string? value))
        {
            int index = value.IndexOf('/');
            if (index >= 0)
            {
                return value[..index];
            }
        }
        return "ERROR - big craftable not found!";
    }

    /// <summary>
    /// Gets the translated name of a bigcraftable.
    /// </summary>
    /// <param name="bigCraftableIndex">Index of the bigcraftable.</param>
    /// <returns>Name of the bigcraftable.</returns>
    public static string GetBigCraftableTranslatedName(this int bigCraftableIndex)
    {
        if (Game1.bigCraftablesInformation?.TryGetValue(bigCraftableIndex, out string? value) == true)
        {
            int index = value.LastIndexOf('/');
            if (index >= 0 && index < value.Length - 1)
            {
                return value[(index + 1)..];
            }
        }
        return "ERROR - big craftable not found!";
    }

    /// <summary>
    /// Consumes a recipe by teaching the player the recipe.
    /// </summary>
    /// <param name="obj">The object instance.</param>
    /// <returns>True if the recipe was taught, false otherwise.</returns>
    public static bool ConsumeRecipeImpl(this SObject obj)
    {
        Guard.IsNotNull(obj);
        Guard.IsNotNull(Game1.player);

        if (obj.IsRecipe)
        {
            string recipeName = obj.Name;

            // vanilla removes the word "Recipe" from the end
            // because ???
            int idx = recipeName.IndexOf("Recipe");
            if (idx > 0)
            {
                recipeName = recipeName[..(idx - 1)];
            }

            return obj.Category == -7
                ? Game1.player.cookingRecipes.TryAdd(recipeName, 0)
                : Game1.player.craftingRecipes.TryAdd(recipeName, 0);
        }
        return false;
    }
}