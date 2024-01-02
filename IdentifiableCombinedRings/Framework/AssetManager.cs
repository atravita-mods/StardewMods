using AtraBase.Collections;
using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.ItemManagement;

using AtraShared.ConstantsAndEnums;

using IdentifiableCombinedRings.DataModels;

using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

namespace IdentifiableCombinedRings.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static readonly Dictionary<RingPair, Lazy<Texture2D>> TextureOverrides = new();

    private static IAssetName RingLocation = null!;

    /// <summary>
    /// Initializes the asset manager.
    /// </summary>
    /// <param name="parser">Game Content Helper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        RingLocation = parser.ParseAssetName("Mods/atravita/IdentifiableCombinedRings/Data");
    }

    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    internal static void OnAssetRequested(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(RingLocation))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, RingDataModel>, AssetLoadPriority.Exclusive);
        }
    }

    /// <summary>
    /// Gets the override texture associated with a ring pair.
    /// </summary>
    /// <param name="first">One ring.</param>
    /// <param name="second">The other ring.</param>
    /// <returns>Override texture if it exists.</returns>
    internal static Texture2D? GetOverrideTexture(string first, string second)
    {
        if (TextureOverrides.Count == 0)
        {
            return null;
        }

        RingPair pair = first.CompareTo(second) > 0
                ? new(second, first)
                : new(first, second);

        return TextureOverrides.GetValueOrDefault(pair)?.Value;
    }

    /// <summary>
    /// Loads in the ring overrides.
    /// </summary>
    internal static void Load()
    {
        TextureOverrides.Clear();

        Dictionary<string, RingDataModel> models = Game1.content.Load<Dictionary<string, RingDataModel>>(RingLocation.BaseName);
        foreach (RingDataModel model in models.Values)
        {
            if (model.RingIdentifiers is not string identifiers
                || !identifiers.TrySplitOnce(',', out ReadOnlySpan<char> first, out ReadOnlySpan<char> second)
                || second.Contains(','))
            {
                ModEntry.ModMonitor.Log($"'{model.RingIdentifiers ?? string.Empty}' was not a valid identifier set, skipping.", LogLevel.Warn);
                continue;
            }

            if (string.IsNullOrWhiteSpace(model.TextureLocation))
            {
                ModEntry.ModMonitor.Log($"Texture cannot be null or whitespace", LogLevel.Warn);
                continue;
            }

            if (!TryParseToRing(first, out string? firstring) || !TryParseToRing(second, out string? secondring))
            {
                ModEntry.ModMonitor.Log($"'{identifiers}' refer to rings that could not be resolved, skipping.", LogLevel.Warn);
                continue;
            }

            if (firstring == secondring)
            {
                ModEntry.ModMonitor.Log($"'{identifiers}' refer to the same ring, skipping.", LogLevel.Warn);
                continue;
            }

            RingPair pair = firstring.CompareTo(secondring) > 0
                ? new(secondring, firstring)
                : new(firstring, secondring);

            TextureOverrides[pair] = new(() => Game1.content.Load<Texture2D>(model.TextureLocation));
        }
    }

    private static bool TryParseToRing(ReadOnlySpan<char> span, [NotNullWhen(true)] out string? ringID)
    {
        string id = ringID = span.Trim().ToString();
        if (!Game1.objectData.TryGetValue(id, out StardewValley.GameData.Objects.ObjectData? data))
        {
            ringID = DataToItemMap.GetID(ItemTypeEnum.Ring, span.ToString());
            if (ringID is not null)
            {
                _ = Game1.objectData.TryGetValue(id, out data);
            }
        }

        return ringID is not null && (data?.Type == "Ring" || data?.Category == SObject.ringCategory);
    }
}
