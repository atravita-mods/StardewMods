using AtraShared.Utils;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TokenizableStrings;

namespace AtraCore.HarmonyPatches;

/// <summary>
/// Holds patches to apply custom category names and colors.
/// </summary>
[HarmonyPatch]
internal static class CategoryPatches
{
    // unqualified item ID to override.
    private static readonly Dictionary<string, (string? title, Color? color, string? icon)> _cache = [];

    // category (negative int.)
    private static readonly Dictionary<int, (string? title, Color? color, string? icon)> _category_cache = [];

    /// <summary>
    /// Clears the cache.
    /// </summary>
    internal static void Reset()
    {
        _cache.Clear();
        _category_cache.Clear();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SObject), nameof(SObject.getCategoryName))]
    private static bool OverrideCategoryName(SObject __instance, ref string __result)
    {
        (string? title, Color? _, string? _) = __instance.Evaluate();

        if (title is not null)
        {
            __result = title;
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SObject), nameof(SObject.getCategoryColor))]
    private static bool OverrideCategoryColor(SObject __instance, ref Color __result)
    {
        (string? _, Color? color, string? _) = __instance.Evaluate();

        if (color is not null)
        {
            __result = color.Value;
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CraftingRecipe), nameof(CraftingRecipe.getSpriteIndexFromRawIndex))]
    private static bool OverrideCategoryIcon(string item_id, ref string __result)
    {
        if (int.TryParse(item_id, out int cat) && cat < 0)
        {
            (string? _, Color? _, string? iconS) = EvaluateCategory(cat);
            if (iconS is not null)
            {
                __result = iconS;
                return false;
            }

            return true;
        }

        ParsedItemData? data = ItemRegistry.GetDataOrErrorItem(item_id);
        if (data?.ItemType is not ObjectDataDefinition)
        {
            return true;
        }

        (string? _, Color? _, string? icon) = Evaluate(data.ItemId, data.Category);

        ModEntry.ModMonitor.VerboseLog($"Overriding {data.ItemId}/{data.Category} with icon {icon}.");

        if (icon is not null)
        {
            ParsedItemData? iconData = ItemRegistry.GetData(icon);
            if (iconData is not null)
            {
                __result = iconData.QualifiedItemId;
                return false;
            }
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CraftingRecipe), nameof(CraftingRecipe.getNameFromIndex))]
    private static bool OverrideCategoryCrafting(string item_id, ref string __result)
    {
        if (int.TryParse(item_id, out int cat) && cat < 0)
        {
            (string? titleC, Color? _, string? _) = EvaluateCategory(cat);
            if (titleC is not null)
            {
                __result = titleC;
                return false;
            }

            return true;
        }

        ParsedItemData? data = ItemRegistry.GetDataOrErrorItem(item_id);
        if (data?.ItemType is not ObjectDataDefinition)
        {
            return true;
        }

        (string? title, Color? _, string? _) = Evaluate(data.ItemId, data.Category);

        ModEntry.ModMonitor.VerboseLog($"Overriding {data.ItemId}/{data.Category} with title {title}.");

        if (title is not null)
        {
            __result = I18n.Any(title);
            return false;
        }

        return true;
    }

    private static (string? title, Color? color, string? icon) Evaluate(this SObject obj)
    {
        if (!obj.HasTypeObject())
        {
            return (null, null, null);
        }

        return Evaluate(obj.ItemId, obj.Category);
    }

    private static (string? title, Color? color, string? icon) Evaluate(string unqualified, int category)
    {
        if (!_cache.TryGetValue(unqualified, out (string? title, Color? color, string? icon) val))
        {
            string? title = null;
            Color? color = null;
            string? icon = null;

            if (Game1.objectData.TryGetValue(unqualified, out ObjectData? objectData) && objectData.CustomFields is { } fields)
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
                if (fields.TryGetValue("atravita.CategoryIconOverride", out string? iconS) && iconS is not null)
                {
                    icon = iconS;
                }
            }

            _cache[unqualified] = val = (title, color, icon);
        }

        if (val.title is not null && val.color is not null && val.icon is not null)
        {
            return val;
        }

        (string? title, Color? color, string? icon) cat = EvaluateCategory(category);
        return (val.title ?? cat.title, val.color ?? cat.color, val.icon ?? cat.icon);
    }

    private static (string? title, Color? color, string? icon) EvaluateCategory(int category)
    {
        if (!_category_cache.TryGetValue(category, out (string? title, Color? color, string? icon) val))
        {
            string? cat_title = null;
            Color? cat_color = null;
            string? cat_icon = null;

            if (AssetManager.GetCategoryExtension(category) is { } categoryOverride)
            {
                if (categoryOverride.CategoryNameOverride is string tokenizedTitle && TokenParser.ParseText(tokenizedTitle) is string proposedTitle
                    && !string.IsNullOrWhiteSpace(proposedTitle))
                {
                    cat_title = proposedTitle;
                }
                if (categoryOverride.CategoryColorOverride is string sColor && ColorHandler.TryParseColor(sColor, out Color proposedColor))
                {
                    cat_color = proposedColor;
                }
                if (categoryOverride.CategoryItemOverride is string sIcon)
                {
                    cat_icon = sIcon;
                }
            }

            _category_cache[category] = val = (cat_title, cat_color, cat_icon);
        }

        return val;
    }
}
