namespace CatGiftsRedux.Framework;

using AtraBase.Collections;
using AtraBase.Models.WeightedRandom;

using AtraCore.Framework.ItemManagement;

using AtraShared.ItemManagement;
using AtraShared.Utils;
using AtraShared.Wrappers;

using StardewModdingAPI.Events;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static IAssetName path = null!;

    private static IAssetName dataObjects = null!;

    private static WeightedManager<ItemRecord?>? manager;
    private static string[]? rings;

    internal static string[] Rings
    {
        get
        {
            rings ??= Game1Wrappers.ObjectData.Where(static kvp => !ItemHelperUtils.RingFilter(kvp.Key, kvp.Value))
                                              .Select(static kvp => kvp.Key).ToArray();
            return rings;
        }
    }

    /// <summary>
    /// Initializes the asset manager.
    /// </summary>
    /// <param name="parser">Game content helper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        path = parser.ParseAssetName("Mods/atravita/CatGiftsRedux/Data");
        dataObjects = parser.ParseAssetName("Data/Objects");
    }

    /// <summary>
    /// Applies the asset edits.
    /// </summary>
    /// <param name="e">Asset requested event args.</param>
    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(path))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, WeightedItemData>, AssetLoadPriority.Exclusive);
        }
    }

    internal static void Reset(IReadOnlySet<IAssetName>? assets)
    {
        if (assets is null || assets.Contains(path))
        {
            manager = null;
        }

        if (assets is null || assets.Contains(dataObjects))
        {
            rings = null;
        }
    }

    /// <summary>
    /// Picks an item from the mod-added list.
    /// </summary>
    /// <param name="random">The seeded random to use.</param>
    /// <returns>An item, or null to skip.</returns>
    internal static Item? Pick(Random random)
    {
        ModEntry.ModMonitor.Log("Picking from mod added data.");

        Dictionary<string, WeightedItemData>.ValueCollection? data = Game1.temporaryContent.Load<Dictionary<string, WeightedItemData>>(path.BaseName).Values;

        if (data.Count == 0)
        {
            return null;
        }

        manager ??= new(data.Select(item => new WeightedItem<ItemRecord?>(item.Weight, item.Item)));

        if (!manager.GetValue(random).TryGetValue(out ItemRecord? entry) || entry is null)
        {
            return null;
        }

        string? id = entry.Identifier;
        if (!DataToItemMap.IsValidId(entry.Type, entry.Identifier))
        {
            id = DataToItemMap.GetID(entry.Type, entry.Identifier);
        }

        if (id is not null)
        {
            return ItemUtils.GetItemFromIdentifier(entry.Type, id);
        }
        return null;
    }
}
