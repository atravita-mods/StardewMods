namespace TapGiantCrops.Framework;

using AtraBase.Collections;

using AtraCore.Framework.ItemManagement;

using AtraShared.ConstantsAndEnums;

using StardewModdingAPI.Events;

/// <summary>
/// A data class indicating an SObject with optional preserve values.
/// </summary>
public sealed class ObjectDefinition
{
    /// <summary>
    /// Gets or sets identifier for the object.
    /// </summary>
    public string Object { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets identifier for the preserve.
    /// </summary>
    public string? Preserve { get; set; } = null;

    /// <summary>
    /// Gets or sets a price override for the object.
    /// </summary>
    public string? PriceOverride { get; set; } = null;

    /// <summary>
    /// Gets or sets a duration override for the object.
    /// </summary>
    public string? DurationOverride { get; set; } = null;
}

internal readonly record struct OverrideObject(SObject? obj, int? duration);

/// <summary>
/// Manages assets for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1214:Readonly fields should appear before non-readonly fields", Justification = "Keeping relevant fields together.")]
internal static class AssetManager
{
    private static IAssetName assetName = null!;

    private static readonly Dictionary<string, OverrideObject> OverridesCache = new();
    private static Lazy<Dictionary<string, ObjectDefinition>> overrides = new(() => Game1.content.Load<Dictionary<string, ObjectDefinition>>(assetName.BaseName));

    /// <summary>
    /// Initializes the asset manager.
    /// </summary>
    /// <param name="parser">Game content helper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        assetName = parser.ParseAssetName("Mods/atravita.TapGiantCrops/TappedObjectOverride");
    }

    /// <summary>
    /// Gets the relevant override item given a certain input parent sheet index.
    /// </summary>
    /// <param name="qualID">The qualified item ID.</param>
    /// <returns>The tapper's product if an override is found.</returns>
    internal static OverrideObject? GetOverrideItem(string qualID)
    {
        if (OverridesCache.TryGetValue(qualID, out OverrideObject obj))
        {
            return obj;
        }

        if (overrides.Value.TryGetValue(qualID, out ObjectDefinition? objectDefinition))
        {
            var objId = objectDefinition.Object;
            if (!Game1.objectData.ContainsKey(objId))
            {
                objId = DataToItemMap.GetID(ItemTypeEnum.SObject, objectDefinition.Object);
            }

            if (objId is null)
            {
                ModEntry.ModMonitor.Log($"{objectDefinition.Object} corresponds to an object that could not be resolved", LogLevel.Warn);
                return null; // not valid
            }

            SObject? @object = null;

            if (objectDefinition.Preserve is not null)
            {
                @object = GeneratePreservesIfPossible(objectDefinition.Preserve, objId);
            }

            @object ??= new(objId, 1);

            if (objectDefinition.PriceOverride is not null && int.TryParse(objectDefinition.PriceOverride, out int priceOverride) && priceOverride > 0)
            {
                @object.Price = priceOverride;
            }

            int? duration = null;
            if (objectDefinition.DurationOverride is not null && int.TryParse(objectDefinition.DurationOverride, out int durationOverride) && durationOverride > 0)
            {
                duration = durationOverride;
            }

            OverrideObject ret = new(@object, duration);
            OverridesCache[qualID] = ret;
            return ret;
        }

        return null;
    }

    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    internal static void Load(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(assetName))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<int, ObjectDefinition>, AssetLoadPriority.Exclusive);
        }
    }

    /// <summary>
    /// Handles invalidations.
    /// </summary>
    /// <param name="assets">The assets to invalidate, or null to invalidate anyways.</param>
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (assets is null || assets.Contains(assetName))
        {
            if (overrides.IsValueCreated)
            {
                overrides = new(() => Game1.content.Load<Dictionary<string, ObjectDefinition>>(assetName.BaseName));
            }
            OverridesCache.Clear();
        }
    }

    private static SObject? GeneratePreservesIfPossible(string? preserveId, string? objId)
    {
        if (preserveId is null)
        {
            return null;
        }

        if (!Game1.objectData.ContainsKey(preserveId))
        {
            preserveId = DataToItemMap.GetID(ItemTypeEnum.SObject, preserveId);
        }

        if (preserveId is null)
        {
            return null;
        }

        SObject.PreserveType? preserveType = objId switch
        {
            "447" => SObject.PreserveType.AgedRoe,
            "340" => SObject.PreserveType.Honey,
            "344" => SObject.PreserveType.Jelly,
            "350" => SObject.PreserveType.Juice,
            "342" => SObject.PreserveType.Pickle,
            "812" => SObject.PreserveType.Roe,
            "348" => SObject.PreserveType.Wine,
            _ => null,
        };

        if (preserveType is not null)
        {
            return ItemRegistry.GetObjectTypeDefinition().CreateFlavoredItem(preserveType.Value, new SObject(preserveId, 1));
        }

        return null;
    }
}
