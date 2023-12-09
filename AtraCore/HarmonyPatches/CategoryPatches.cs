using AtraShared.Utils;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Extensions;
using StardewValley.GameData.Objects;

namespace AtraCore.HarmonyPatches;

/// <summary>
/// Holds patches against <see cref="SObject"/> to apply custom category names and colors.
/// </summary>
[HarmonyPatch(typeof(SObject))]
internal static class CategoryPatches
{
    // qualified item ID to override.
    private static readonly Dictionary<string, (string? title, Color? color)> _cache = [];

    /// <summary>
    /// Clears the cache.
    /// </summary>
    internal static void Reset() => _cache.Clear();

    [HarmonyPrefix]
    [HarmonyPatch(nameof(SObject.getCategoryName))]
    private static bool OverrideCategoryName(SObject __instance, ref string __result)
    {
        (string? title, Color? _) = __instance.Evaluate();

        if (title is not null)
        {
            __result = title;
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(SObject.getCategoryColor))]
    private static bool OverrideCategoryColor(SObject __instance, ref Color __result)
    {
        (string? _, Color? color) = __instance.Evaluate();

        if (color is not null)
        {
            __result = color.Value;
            return false;
        }

        return true;
    }

    private static (string? title, Color? color) Evaluate(this SObject obj)
    {
        if (!obj.HasTypeObject())
        {
            return (null, null);
        }

        if (_cache.TryGetValue(obj.QualifiedItemId, out (string? title, Color? color) val))
        {
            return val;
        }

        string? title = null;
        Color? color = null;

        if (Game1.objectData.TryGetValue(obj.ItemId, out ObjectData? objectData) && objectData.CustomFields is { } fields)
        {
            if (fields.TryGetValue("atravita.CategoryNameOverride", out string? tokenizedTitle) && TokenParser.ParseText(tokenizedTitle) is string proposedTitle
                && !string.IsNullOrWhiteSpace(proposedTitle))
            {
                title = proposedTitle;
            }
            if (fields.TryGetValue("atravita.CategoryColorOverride", out string? sColor) && ColorHandler.TryParseColor(sColor, out Color proposedColor))
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
            if (categoryOverride.CategoryColorOverride is string sColor && ColorHandler.TryParseColor(sColor, out Color proposedColor))
            {
                color = proposedColor;
            }
        }

        return _cache[obj.QualifiedItemId] = (title, color);
    }
}
