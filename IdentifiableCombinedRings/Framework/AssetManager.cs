using AtraBase.Collections;
using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.ItemManagement;

using AtraShared.ConstantsAndEnums;

using IdentifiableCombinedRings.DataModels;

using StardewModdingAPI.Events;

namespace IdentifiableCombinedRings.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static IAssetName RingLocation = null!;

    private static Dictionary<RingPair, string> textureOverrides = new();

    internal static void Initialize(IGameContentHelper parser)
    {
        RingLocation = parser.ParseAssetName("Mods/atravita/IdentifiableCombinedRings/Data");
    }

    internal static void OnAssetRequested(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(RingLocation))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, RingDataModel>, AssetLoadPriority.Exclusive);
        }
    }

    internal static void Load()
    {
        textureOverrides.Clear();

        Dictionary<string, RingDataModel> models = Game1.content.Load<Dictionary<string, RingDataModel>>(RingLocation.BaseName);
        foreach (RingDataModel model in models.Values)
        {
            if (model.RingIdentifiers is not string identifiers
                || !identifiers.TrySplitOnce(',', out ReadOnlySpan<char> first, out ReadOnlySpan<char> second)
                || second.Contains(','))
            {
                Globals.ModMonitor.Log($"'{model.RingIdentifiers ?? string.Empty}' was not a valid identifier set, skipping.", LogLevel.Error);
                continue;
            }

            if (!TryParseToRing(first, out int firstring) || !TryParseToRing(second, out int secondring))
            {
                Globals.ModMonitor.Log($"'{identifiers}' refer to rings that could not be resolved, skipping.", LogLevel.Warn);
                continue;
            }

            if (firstring == secondring)
            {
                Globals.ModMonitor.Log($"'{identifiers}' refer to the same ring, skipping.", LogLevel.Warn);
                continue;
            }

            // swap so firstring is always lower.
            if (firstring > secondring)
            {
                (secondring, firstring) = (firstring, secondring);
            }
        }
    }

    private static bool TryParseToRing(ReadOnlySpan<char> span, out int ringID)
    {
        span = span.Trim();
        if (int.TryParse(span, out ringID) && ringID > 0 && DataToItemMap.IsActuallyRing(ringID))
        {
            return true;
        }

        ringID = DataToItemMap.GetID(ItemTypeEnum.Ring, span.ToString());
        return ringID > 0;
    }
}
