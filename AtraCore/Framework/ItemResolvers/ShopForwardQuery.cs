using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AtraBase.Toolkit.Extensions;

using StardewValley.Internal;

namespace AtraCore.Framework.ItemResolvers;
internal static class ShopForwardQuery
{
    private static readonly ThreadLocal<Stack<string>> _visited = new(static () => new());

    internal static IEnumerable<ItemQueryResult> Query(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, "arguments should not be null or whitespace");
            yield break;
        }

        string shop = arguments;
        string? filter = null;
        if (arguments.TrySplitOnce(' ', out ReadOnlySpan<char> first, out ReadOnlySpan<char> second))
        {
            shop = first.Trim().ToString();
            filter = second.Trim().ToString();
        }

        if (_visited.Value!.Contains(shop))
        {
            ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, "Broke loop in shop redirect: " + string.Join(", ", _visited.Value));
            yield break;
        }
        _visited.Value.Push(shop);

        if (!DataLoader.Shops(Game1.content).TryGetValue(shop, out var shopData))
        {
            ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, $"Could not find shop {shop} to copy from.");
            yield break;
        }

        HashSet<string>? prev = avoidRepeat ? [] : null;

        foreach ((ISalable item, ItemStockInformation _) in ShopBuilder.GetShopStock(shop, shopData))
        {
            if (avoidItemIds?.Contains(item.QualifiedItemId) == true || prev?.Add(item.QualifiedItemId) == true)
            {
                continue;
            }
            var candidate = item as Item;
            if (candidate is null)
            {
                continue;
            }
            if (candidate.IsRecipe && Game1.player.knowsRecipe(candidate.Name))
            {
                continue;
            }

            if (!GameStateQuery.CheckConditions(filter, targetItem: candidate))
            {
                continue;
            }

            yield return new(candidate);
        }

        if (!_visited.Value.TryPop(out _))
        {
            ModEntry.ModMonitor.Log("Huh, we really didn't expect the stack to be empty, eh?");
            ModEntry.ModMonitor.Log(new StackTrace().ToString());
        }
    }
}
