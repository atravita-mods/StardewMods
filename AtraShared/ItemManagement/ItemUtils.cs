using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using StardewValley.Objects;
using StardewValley.Tools;

namespace AtraShared.ItemManagement;

/// <summary>
/// Methods to help deal with items.
/// </summary>
public static class ItemUtils
{
    /// <summary>
    /// Get an item from the string identifier system.
    /// Returns null if it's not something that can be handled.
    /// </summary>
    /// <param name="type">Identifier string (like "O" or "B").</param>
    /// <param name="id">int id.</param>
    /// <returns>Item if possible.</returns>
    /// <remarks>Carries the no-inlining attribute so other mods can patch this.</remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Item? GetItemFromIdentifier(string type, int id)
    => type switch
    {
        "F" or "f" => Furniture.GetFurnitureInstance(id),
        "O" or "o" => new SObject(id, 1),
        "BL" or "bl" => new SObject(id, 1, isRecipe: true), // big craftable recipes.
        "BO" or "bo" => new SObject(Vector2.Zero, id),
        "BBL" or "bbl" => new SObject(Vector2.Zero, id, isRecipe: true),
        "R" or "r" => new Ring(id),
        "B" or "b" => new Boots(id),
        "W" or "w" => new MeleeWeapon(id),
        "H" or "h" => new Hat(id),
        "C" or "c" => new Clothing(id),
        _ => null,
    };
}
