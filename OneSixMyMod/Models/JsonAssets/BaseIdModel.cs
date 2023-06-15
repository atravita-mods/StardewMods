using System.Drawing;

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