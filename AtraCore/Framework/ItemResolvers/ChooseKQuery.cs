using StardewValley.Delegates;
using StardewValley.Internal;

namespace AtraCore.Framework.ItemResolvers;
internal class ChooseKQuery
{
    /// <inheritdoc cref="ResolveItemQueryDelegate"/>
    internal static IEnumerable<ItemQueryResult> ChooseK(string key, string? arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string>? avoidItemIds, Action<string, string> logError)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, "arguments could not be null or whitespace");
            yield break;
        }

        var args = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (ArgUtility.TryGetInt(args, 0, out var count, out var error))
        {
            ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, error);
            yield break;
        }

        HashSet<string>? prev = avoidRepeat ? new() : null;

        if (args.Length - 1 <= count)
        {
            foreach (var candidate in new ArraySegment<string>(args, 1, args.Length - 1))
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
        }

        var idx = args.Length - 1;
        var final = idx - count;

        var random = context.Random ?? Utility.CreateDaySaveRandom(Utility.GetDeterministicHashCode("choose_k"));

        while (idx > final)
        {
            int j = random.Next(idx + 1);
            var candidate = args[j];

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

            if (j != idx)
            {
                (args[j], args[idx]) = (args[idx], args[j]);
            }

            idx--;
        }
    }
}
