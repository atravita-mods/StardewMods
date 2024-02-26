using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.StringHandler;

using AtraShared.Utils;

using Microsoft.Xna.Framework;

using StardewValley.Delegates;
using StardewValley.Internal;
using StardewValley.Menus;
using StardewValley.Objects;

namespace AtraCore.Framework.ItemResolvers;
internal static class GenericFlavoredItemQuery
{
    /// <inheritdoc cref="ResolveItemQueryDelegate"/>
    internal static IEnumerable<ItemQueryResult> Generate(string key, string? arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string>? avoidItemIds, Action<string, string> logError)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, "arguments should not be null or whitespace");
            return Enumerable.Empty<ItemQueryResult>();
        }

        StreamSplit splits = arguments.StreamSplit();

        if (!splits.MoveNext())
        {
            return ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, "no base ingredient ID provided");
        }

        string baseID = splits.Current;
        StardewValley.ItemTypeDefinitions.ParsedItemData? basedata = ItemRegistry.GetData(baseID);
        if (basedata is null)
        {
            return ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, "base ingredient ID is not a valid object ID.");
        }

        if (!splits.MoveNext())
        {
            return ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, "no flavor ingredient ID provided");
        }

        string ingredientID = splits.Current;
        StardewValley.ItemTypeDefinitions.ParsedItemData data = ItemRegistry.GetData(ingredientID);
        if (data?.QualifiedItemId.StartsWith(ItemRegistry.type_object) != true)
        {
            return ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, "flavor ingredient ID is not a valid object ID.");
        }

        int original_price = Game1.objectData.GetValueOrGetDefault(data.ItemId)?.Price ?? 0;
        double price = 0;
        Color? color = null;

        while (splits.MoveNext())
        {
            ReadOnlySpan<char> arg = splits.Current.Word;
            if (arg.Length < 2 || arg[0] != '@')
            {
                return ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, $"argument '{arg}' is not valid.");
            }

            if (arg.Equals("@color", StringComparison.OrdinalIgnoreCase))
            {
                splits.TrimStart();
                ReadOnlySpan<char> remainder = splits.Remainder;

                if (remainder.Length == 0 || remainder[0] == '@')
                {
                    return ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, $"argument '{arg}' was not given values");
                }

                int idx = remainder.IndexOf('@');
                if (idx > 0)
                {
                    remainder = remainder[..idx];
                }

                if (remainder.Trim().Equals("copy", StringComparison.OrdinalIgnoreCase))
                {
                    Item item = ItemRegistry.Create(data.QualifiedItemId);
                    color = TailoringMenu.GetDyeColor(item);

                    if (color is null)
                    {
                        return ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, $"Flavored item {data.QualifiedItemId} does not have a color to copy.");
                    }
                    splits.Skip(remainder.Length);
                }
                else if (ColorHandler.TryParseColor(remainder.ToString(), out Color proposedColor))
                {
                    color = proposedColor;
                    splits.Skip(remainder.Length);
                }
                else
                {
                    return ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, $"color '{remainder}' does not correspond to a parse-able color");
                }

            }
            else if (arg.Equals("@price", StringComparison.OrdinalIgnoreCase))
            {
                ReadOnlySpan<char> remainder = splits.Remainder.TrimStart();
                if (remainder.Length == 0 || remainder[0] == '@')
                {
                    price = original_price;
                }
                else
                {
                    do
                    {
                        if (!splits.MoveNext())
                        {
                            break;
                        }
                        if (double.TryParse(splits.Current.Word, out double segment))
                        {
                            price *= original_price;
                            price += segment;
                        }
                    }
                    while (remainder.Length > 0 && remainder[0] != '@');
                }
            }
        }

        SObject? result;

        if (color is not null)
        {
            if (!basedata.QualifiedItemId.StartsWith("(O)"))
            {
                return ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, "cannot create a colored object with a non-object type base item.");
            }
            result = new ColoredObject(basedata.ItemId, 1, color.Value);
        }
        else
        {
            result = ItemRegistry.Create(baseID, allowNull: true) as SObject;
        }

        if (result is not null)
        {
            result.Name = $"{data.InternalName} {result.Name}";
            result.Price = (int)price;
            result.preservedParentSheetIndex.Value = data.ItemId;

            return [new(result)];
        }

        return ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, "could not resolve generic flavored item.");
    }
}
