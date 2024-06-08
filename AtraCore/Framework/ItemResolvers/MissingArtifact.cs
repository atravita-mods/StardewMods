namespace AtraCore.Framework.ItemResolvers;

using AtraCore.HarmonyPatches.MuseumOverflow;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework.Input;

using StardewModdingAPI;

using StardewValley;
using StardewValley.Internal;
using StardewValley.Locations;

using static StardewValley.Internal.ItemQueryResolver;

/// <summary>
/// To get missing artifacts.
/// </summary>
internal static class MissingArtifact
{
    /// <summary>
    /// A list of qualified item IDs of artifacts we are missing.
    /// </summary>
    private static List<string>? _missingArtifacts = [];

    /// <summary>
    /// Resets cache, call at save loaded.
    /// </summary>
    internal static void Reset() => _missingArtifacts = null;

    /// <summary>
    /// Gets the unfound museum donation items, in random order.
    /// </summary>
    /// <returns>Museum donation items.</returns>
    internal static IEnumerable<Item> GetRandomUnfoundArtifacts()
    {
        Populate();

        if (_missingArtifacts?.Count is > 0)
        {
            Utility.Shuffle(Random.Shared, _missingArtifacts);

            for (int i = _missingArtifacts.Count - 1; i >= 0; i--)
            {
                string item = _missingArtifacts[i];
                if (!LibraryMuseum.IsItemSuitableForDonation(item, true))
                {
                    _missingArtifacts.RemoveAt(i);
                }

                Item? obj = null;
                try
                {
                    obj = ItemRegistry.Create(item, allowNull: true);
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor.LogError("creating item for missing artifact", ex);
                }

                if (obj is not null)
                {
                    yield return obj;
                }
            }
        }
    }

    /// <inheritdoc cref="StardewValley.Delegates.ResolveItemQueryDelegate"/>
    internal static IEnumerable<ItemQueryResult> Query(string key, string arguments, ItemQueryContext context, bool avoidRepeat, HashSet<string> avoidItemIds, Action<string, string> logError)
    {
        bool foundItem = false;
        foreach (var item in GetRandomUnfoundArtifacts())
        {
            if (avoidItemIds?.Contains(item.QualifiedItemId) == true)
            {
                continue;
            }

            foundItem = true;
            yield return new(item);
        }

        if (foundItem || string.IsNullOrWhiteSpace(arguments))
        {
            yield break;
        }

        ItemQueryResult[] items = TryResolve(arguments, new ItemQueryContext(context));
        foreach (ItemQueryResult? forwarded in items)
        {
            yield return forwarded;
        }
    }

    private static void Populate()
    {
        if (_missingArtifacts is not null)
        {
            return;
        }

        if (Game1.getLocationFromName("ArchaeologyHouse") is not LibraryMuseum musem)
        {
            ModEntry.ModMonitor.Log($"Could not populate - museum not found. What.", LogLevel.Warn);
            return;
        }

        ModEntry.ModMonitor.Log($"Populating unseen artifacts.");
        HashSet<string> artifacts = musem.museumPieces.Values.ToHashSet();

        if (MuseumOverflowPatches.TryGetInventory(out var inventory))
        {
            foreach (var item in inventory)
            {
                artifacts.Add(item.ItemId);
            }
        }

        _missingArtifacts = Game1.objectData.Keys.Where(item => !artifacts.Contains(item) && LibraryMuseum.IsItemSuitableForDonation(item, true))
            .Select(static item => ItemRegistry.ManuallyQualifyItemId(item, ItemRegistry.type_object)).ToList();
    }
}
