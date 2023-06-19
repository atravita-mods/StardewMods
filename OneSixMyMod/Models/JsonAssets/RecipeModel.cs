namespace OneSixMyMod.Models.JsonAssets;

/// <summary>
/// A Json Assets recipe.
/// </summary>
/// <param name="SkillUnlockName"></param>
/// <param name="SkillUnlockLevel"></param>
/// <param name="ResultCount"></param>
/// <param name="Ingredients"></param>
/// <param name="IsDefault"></param>
/// <param name="CanPurchase"></param>
/// <param name="PurchasePrice"></param>
/// <param name="PurchaseFrom"></param>
/// <param name="PurchaseRequirements"></param>
/// <param name="AdditionalPurchaseData"></param>
public record RecipeModel(
    string? SkillUnlockName = null,
    int SkillUnlockLevel = -1,
    int ResultCount = 1,
    Ingredient[]? Ingredients = null,
    bool IsDefault = false,
    bool CanPurchase = false,
    int PurchasePrice = 0,
    string? PurchaseFrom = null,
    string[]? PurchaseRequirements = null,
    PurchaseData[]? AdditionalPurchaseData = null);