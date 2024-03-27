using StardewValley.Delegates;
using StardewValley.Internal;

namespace AtraCore.Framework.ItemResolvers;

/// <summary>
/// Of the n items given, choose k with equal changes.
/// </summary>
internal class ChooseKQuery
{
    /// <inheritdoc cref="ResolveItemQueryDelegate"/>
    internal static IEnumerable<ItemQueryResult> ChooseK(string key, string? arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string>? avoidItemIds, Action<string, string> logError)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, "arguments should not be null or whitespace");
            yield break;
        }

        string[] args = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!ArgUtility.TryGetInt(args, 0, out int count, out string? error))
        {
            ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, error);
            yield break;
        }

        HashSet<string>? prev = avoidRepeat ? [] : null;

        if (args.Length - 1 <= count)
        {
            foreach (string candidate in new ArraySegment<string>(args, 1, args.Length - 1))
            {
                if (avoidItemIds?.Contains(candidate) == true || prev?.Add(candidate) == true)
                {
                    continue;
                }

                if (ItemRegistry.Create(candidate, allowNull: true) is { } item)
                {
                    yield return new(item);
                }
                else
                {
                    ModEntry.ModMonitor.Log($"{candidate} does not correspond to a valid item.", LogLevel.Trace);
                }
            }
            yield break;
        }

        int idx = args.Length - 1;
        int final = idx - count;

        Random random = context.Random ?? Utility.CreateDaySaveRandom(Game1.hash.GetDeterministicHashCode("choose_k"));

        while (idx > final)
        {
            int j = random.Next(1, idx + 1);
            string candidate = args[j];

            if (avoidItemIds?.Contains(candidate) != true && prev?.Add(candidate) != true)
            {
                if (ItemRegistry.Create(candidate, allowNull: true) is { } item)
                {
                    yield return new(item);
                }
                else
                {
                    ModEntry.ModMonitor.Log($"{candidate} does not correspond to a valid item.", LogLevel.Trace);
                }
            }

            if (j != idx)
            {
                (args[j], args[idx]) = (args[idx], args[j]);
            }
            idx--;
        }
    }
}
