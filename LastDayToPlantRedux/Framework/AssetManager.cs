using AtraBase.Collections;
using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.ItemManagement;

using AtraShared.ConstantsAndEnums;
using AtraShared.Wrappers;

using StardewModdingAPI.Events;

namespace LastDayToPlantRedux.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static readonly string MailFlag = "atravita_LastDayLetter";
    private static IAssetName dataMail = null!;

    // denylist and allowlist
    private static IAssetName accessLists = null!;
    private static bool accessProcessed = false;
    private static readonly HashSet<int> AllowedFertilizers = new();
    private static readonly HashSet<int> DeniedFertilizers = new();
    private static readonly HashSet<int> AllowedSeeds = new();
    private static readonly HashSet<int> DeniedSeeds = new();

    /// <summary>
    /// The data asset for objects.
    /// </summary>
    private static IAssetName objectInfoName = null!;

    /// <summary>
    /// Gets the data asset for Data/crops.
    /// </summary>
    internal static IAssetName CropName { get; private set; } = null!;

    /// <summary>
    /// Initializes the asset manager.
    /// </summary>
    /// <param name="parser">the game content parser.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        dataMail = parser.ParseAssetName("Data/mail");
        CropName = parser.ParseAssetName("Data/Crops");
        objectInfoName = parser.ParseAssetName("Data/ObjectInformation");
        accessLists = parser.ParseAssetName("Mods/atravita.LastDayToPlantRedux/AccessControl");
    }

    /// <summary>
    /// Applies asset edits for this mod.
    /// </summary>
    /// <param name="e">event args.</param>
    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(accessLists))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, string>, AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(dataMail))
        {
            e.Edit(
            static (asset) =>
            {
                var data = asset.AsDictionary<string, string>().Data;
                data[MailFlag] = "";
            }, AssetEditPriority.Late);
        }
    }

    /// <summary>
    /// Listens for cache invalidations and empties the relevant caches if needed.
    /// </summary>
    /// <param name="e">Event args.</param>
    internal static void InvalidateCache(AssetsInvalidatedEventArgs e)
    {
        if (e.NamesWithoutLocale.Contains(CropName))
        {
            CropAndFertilizerManager.RequestInvalidateCrops();
            AssetManager.accessProcessed = false;
        }
        if (e.NamesWithoutLocale.Contains(objectInfoName))
        {
            CropAndFertilizerManager.RequestInvalidateFertilizers();
            AssetManager.accessProcessed = false;
        }
        if (e.NamesWithoutLocale.Contains(accessLists))
        {
            AssetManager.accessProcessed = false;
        }
    }

    private static void ProcessAccessLists()
    {
        if (AssetManager.accessProcessed)
        {
            return;
        }

        AssetManager.accessProcessed = true;

        AllowedFertilizers.Clear();
        DeniedFertilizers.Clear();
        AllowedSeeds.Clear();
        DeniedSeeds.Clear();

        foreach (var (item, access) in Game1.content.Load<Dictionary<string, string>>(AssetManager.accessLists.BaseName))
        {
            if (!int.TryParse(item, out int id))
            {
                id = DataToItemMap.GetID(ItemTypeEnum.SObject, item);
            }

            if (id < -1 || !Game1Wrappers.ObjectInfo.TryGetValue(id, out var data))
            {
                ModEntry.ModMonitor.Log($"{item} could not be resolved, skipping");
                continue;
            }

            var cat = data.GetNthChunk('/', SObject.objectInfoTypeIndex);
            var index = cat.GetIndexOfWhiteSpace();
            if (index < 0 || !int.TryParse(cat[(index + 1)..], out int type))
            {
                ModEntry.ModMonitor.Log($"{item} with {id} does not appear to be a seed or fertilizer, skipping.");
                continue;
            }

            bool isAllow = access.AsSpan().Trim().Equals("Allow", StringComparison.OrdinalIgnoreCase);
            bool isDeny = access.AsSpan().Trim().Equals("Deny", StringComparison.OrdinalIgnoreCase);

            if (!isAllow && !isDeny)
            {
                ModEntry.ModMonitor.Log($"Invalid access term {access}, skipping");
                continue;
            }
            else if (isAllow && isDeny)
            {
                ModEntry.ModMonitor.Log($"Duplicate access term {access}, skipping");
                continue;
            }

            switch (type)
            {
                case SObject.SeedsCategory:
                    if (isAllow)
                    {
                        AllowedSeeds.Add(id);
                    }
                    else if (isDeny)
                    {
                        DeniedSeeds.Add(id);
                    }
                    break;
                case SObject.fertilizerCategory:
                    if (isAllow)
                    {
                        AllowedFertilizers.Add(id);
                    }
                    else if (isDeny)
                    {
                        DeniedFertilizers.Add(id);
                    }
                    break;
                default:
                    ModEntry.ModMonitor.Log($"{item} with {id} is type {type}, not a seed or fertilizer, skipping.");
                    break;
            }
        }
    }
}
