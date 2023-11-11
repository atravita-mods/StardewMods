using AtraShared.Utils;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Extensions;

namespace AtraCore.HarmonyPatches;

/// <summary>
/// Holds patches against <see cref="SObject"/> to apply custom category names and colors.
/// </summary>
[HarmonyPatch(typeof(SObject))]
internal static class CategoryPatches
{
    // qualified item ID to override.
    private static readonly Dictionary<string, (string? title, Color? color)> _cache = new();

    /// <summary>
    /// Clears the cache.
    /// </summary>
    internal static void Reset() => _cache.Clear();



    private static (string? title, Color? color)? Evaluate(this SObject obj)
    {
        if (!obj.HasTypeObject())
        {
            return (null, null);
        }

        if (_cache.TryGetValue(obj.QualifiedItemId, out var val))
        {
            return val;
        }

        string? title = null;
        Color? color = null;

        if (Game1.objectData.TryGetValue(obj.ItemId, out var objectData) && objectData.CustomFields is { } fields)
        {
            if (fields.TryGetValue("atravita.CategoryNameOverride", out var tokenizedTitle) && TokenParser.ParseText(tokenizedTitle) is string proposedTitle
                && !string.IsNullOrWhiteSpace(proposedTitle))
            {
                title = proposedTitle;
            }
            if (fields.TryGetValue("atravita.CategoryColorOverride", out var sColor) && ColorHandler.TryParseColor(sColor, out var proposedColor))
            {
                color = proposedColor;
            }
        }

        if (title is not null && color is not null)
        {
            return _cache[obj.QualifiedItemId] = (title, color);
        }

        if (AssetManager.GetCategoryExtension(obj.Category) is { } categoryOverride)
        {
            if (categoryOverride.CategoryNameOverride is string tokenizedTitle && TokenParser.ParseText(tokenizedTitle) is string proposedTitle
                && !string.IsNullOrWhiteSpace(proposedTitle))
            {
                title = proposedTitle;
            }
            if (categoryOverride.CategoryColorOverride is string sColor && ColorHandler.TryParseColor(sColor, out var proposedColor))
            {
                color = proposedColor;
            }
        }

        return _cache[obj.QualifiedItemId] = (title, color);
    }
}
