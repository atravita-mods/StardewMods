using AtraBase.Collections;

using AtraCore.Framework.ItemManagement;

using AtraShared.ConstantsAndEnums;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace TapGiantCrops.Framework;

public sealed class ObjectDefinition
{
    public string Object { get; set; } = string.Empty;

    public string? Preserve { get; set; } = null;
}

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static readonly string ASSETPATH = PathUtilities.NormalizeAssetName("Mods/atravita.TapGiantCrops/TappedObjectOverride");

    private static Lazy<Dictionary<int, ObjectDefinition>> overrides = new(() => Game1.content.Load<Dictionary<int, ObjectDefinition>>(ASSETPATH));
    private static readonly Dictionary<int, SObject> overridesCache = new();

    private static IAssetName? assetName = null;

    private static IAssetName AssetName =>
        assetName ??= ModEntry.GameContentHelper.ParseAssetName(ASSETPATH);

    internal static SObject? GetOverrideItem(int input)
    {
        if (overridesCache.TryGetValue(input, out var obj))
        {
            return obj.getOne() as SObject;
        }

        if (overrides.Value.TryGetValue(input, out var objectDefinition))
        {
            if (!int.TryParse(objectDefinition.Object, out int id))
            {
                id = DataToItemMap.GetID(ItemTypeEnum.SObject, objectDefinition.Object);
            }

            if (id < 0)
            {
                ModEntry.ModMonitor.Log($"{objectDefinition.Object} corresponds to an object that could not be resolved", LogLevel.Warn);
                return null; // not valid
            }

            SObject ret = new(id, 1);

            if (objectDefinition.Preserve is not null)
            {
                if (!int.TryParse(objectDefinition.Preserve, out int preserveId))
                {
                    preserveId = DataToItemMap.GetID(ItemTypeEnum.SObject, objectDefinition.Preserve);
                }

                if (preserveId > 0)
                {
                    ret.preservedParentSheetIndex.Value = preserveId;
                }
            }

            overridesCache[input] = ret;
            return ret.getOne() as SObject;
        }

        return null;
    }

    /// <summary>
    /// Loads assets for this mod.
    /// </summary>
    /// <param name="e">AssetRequestedEventArgs.</param>
    internal static void Load(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(ASSETPATH))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<int, string>, AssetLoadPriority.Exclusive);
        }
    }

    /// <summary>
    /// Handles invalidations.
    /// </summary>
    /// <param name="assets">The assets to invalidate, or null to invalidate anyways.</param>
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (assets is null || assets.Contains(AssetName))
        {
            if (overrides.IsValueCreated)
            {
                overrides = new(() => Game1.content.Load<Dictionary<int, ObjectDefinition>>(ASSETPATH));
            }
            overridesCache.Clear();
        }
    }
}
