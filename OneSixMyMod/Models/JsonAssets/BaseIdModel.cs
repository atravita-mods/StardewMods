namespace OneSixMyMod.Models.JsonAssets;

/// <summary>
/// The base JA data model, which everything else inherits from.
/// </summary>
/// <param name="Name">Internal name of the item.</param>
/// <param name="EnableWithMod">Mod required to load this item.</param>
/// <param name="DisableWithMod">Mod that will disable this item.</param>
public record BaseIdModel(
    string Name,
    string? EnableWithMod,
    string? DisableWithMod);

/// <summary>
/// An ingredient.
/// </summary>
/// <param name="Object">The object to use.</param>
/// <param name="Count">How many are needed.</param>
public record Ingredient(
    object Object,
    int Count);

/// <summary>
/// Used for additional purchase info from JA.
/// </summary>
/// <param name="PurchasePrice">Cost of the item.</param>
/// <param name="PurchaseFrom">Who to purchase it from.</param>
/// <param name="PurchaseRequirements">The requirements.</param>
public record PurchaseData(
    int PurchasePrice,
    string? PurchaseFrom,
    string[]? PurchaseRequirements);