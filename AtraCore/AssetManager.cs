namespace AtraCore;

using AtraBase.Collections;
using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.Models;
using AtraCore.Models;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

/// <summary>
/// Handles asset management for this mod.
/// </summary>
internal static class AssetManager
{
    private static readonly HashSet<string> eventLocations = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string dataEvents = PathUtilities.NormalizeAssetName("Data/Events") + "/";

    private static IAssetName prismatic = null!;
    private static IAssetName equipData = null!;
    private static IAssetName equipBuffIcons = null!;

    private static Lazy<Dictionary<string, EquipmentExtModel>> _ringData = new(
        static () => Game1.content.Load<Dictionary<string, EquipmentExtModel>>(AtraCoreConstants.EquipData));

    private static Lazy<Texture2D> _ringTextures = new(
        static () => Game1.content.Load<Texture2D>(equipBuffIcons.BaseName));

    /// <summary>
    /// Gets the additional ring buff icons.
    /// </summary>
    internal static Texture2D RingTextures => _ringTextures.Value;

    /// <summary>
    /// Initializes the asset manager.
    /// </summary>
    /// <param name="parser">GameContentHelper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        prismatic = parser.ParseAssetName(AtraCoreConstants.PrismaticMaskData);
        equipData = parser.ParseAssetName(AtraCoreConstants.EquipData);
        equipBuffIcons = parser.ParseAssetName("Mods/atravita/EquipBuffIcons");

        // check and populate the event locations.
        foreach (string? location in new[] { "AdventureGuild", "Blacksmith", "WitchHut", "WitchSwamp", "Summit" })
        {
            try
            {
                _ = parser.Load<Dictionary<string, string>>(dataEvents + location);
            }
            catch (ContentLoadException)
            {
                ModEntry.ModMonitor.DebugOnlyLog($"Adding location {location} to event file loaders.");
                eventLocations.Add(location);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError($"checking {location}'s event file", ex);
            }
        }

        ModEntry.ModMonitor.Log($"Checked event data, adding {eventLocations.Count}");
    }

    /// <summary>
    /// Gets the prismatic models data asset.
    /// </summary>
    /// <returns>The prismatic models data asset.</returns>
    internal static Dictionary<string, DrawPrismaticModel>? GetPrismaticModels()
    {
        try
        {
            return Game1.temporaryContent.Load<Dictionary<string, DrawPrismaticModel>>(AtraCoreConstants.PrismaticMaskData);
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("loading prismatic mask data", ex);
        }
        return null;
    }

    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(prismatic))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, DrawPrismaticModel>, AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(equipData))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, EquipmentExtModel>, AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(equipBuffIcons))
        {
            e.LoadFromModFile<Texture2D>("assets/BuffIcons.png", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.StartsWith(dataEvents, false, false))
        {
            string loc = e.NameWithoutLocale.BaseName.GetNthChunk('/', 2).ToString();
            if (eventLocations.Contains(loc))
            {
                e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, string>, AssetLoadPriority.Low - 1000);
            }
        }
    }

    /// <inheritdoc cref="IContentEvents.AssetsInvalidated"/>
    internal static void Invalidate(IReadOnlySet<IAssetName>? assets = null)
    {
        if (assets is null || assets.Contains(equipData))
        {
            if (_ringData.IsValueCreated)
            {
                _ringData = new(static () => Game1.content.Load<Dictionary<string, EquipmentExtModel>>(AtraCoreConstants.EquipData));
            }
        }
        if (assets is null || assets.Contains(equipBuffIcons))
        {
            if (_ringTextures.IsValueCreated)
            {
                _ringTextures = new(static () => Game1.content.Load<Texture2D>(equipBuffIcons.BaseName));
            }
        }
    }

    /// <summary>
    /// Gets the data associated with a specific ring, if it exists.
    /// </summary>
    /// <param name="ringID">The ring's Id.</param>
    /// <returns>The ring data, if it exists.</returns>
    internal static EquipmentExtModel? GetRingData(string ringID)
        => _ringData.Value.GetValueOrDefault(ringID);
}
