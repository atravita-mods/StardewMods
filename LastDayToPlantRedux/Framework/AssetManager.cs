using AtraBase.Collections;

using StardewModdingAPI.Events;

namespace LastDayToPlantRedux.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    // accessors
    internal static HashSet<int> AllowedFertilizers
    {
        get
        {
            ProcessAccessLists();
            return allowedFertilizers;
        }
    }

    internal static HashSet<int> DeniedFertilizers
    {
        get
        {
            ProcessAccessLists();
            return deniedFertilizers;
        }
    }

    internal static HashSet<int> AllowedSeeds
    {
        get
        {
            ProcessAccessLists();
            return allowedSeeds;
        }
    }

    internal static HashSet<int> DeniedSeeds
    {
        get
        {
            ProcessAccessLists();
            return deniedSeeds;
        }
    }

    private static readonly string MailFlag = "atravita_LastDayLetter";
    private static IAssetName dataMail = null!;

    // denylist and allowlist
    private static readonly HashSet<int> allowedFertilizers = new();
    private static readonly HashSet<int> deniedFertilizers = new();
    private static readonly HashSet<int> allowedSeeds = new();
    private static readonly HashSet<int> deniedSeeds = new();
    private static IAssetName accessLists = null!;
    private static bool accessProcessed = false;

    private static string Message = string.Empty;

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

    internal static void UpdateOnDayStart()
    {
        Message = CropAndFertilizerManager.GenerateMessageString();
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
        else if (e.NameWithoutLocale.IsEquivalentTo(dataMail) && !string.IsNullOrWhiteSpace(Message))
        {
            e.Edit(
            static (asset) =>
            {
                var data = asset.AsDictionary<string, string>().Data;
                data[MailFlag] = Message;
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

        allowedFertilizers.Clear();
        deniedFertilizers.Clear();
        allowedSeeds.Clear();
        deniedSeeds.Clear();

        foreach ((string item, string access) in Game1.content.Load<Dictionary<string, string>>(AssetManager.accessLists.BaseName))
        {
            (int id, int type)? tup = LDUtils.ResolveIDAndType(item);
            if (tup is null)
            {
                continue;
            }
            int id = tup.Value.id;
            int type = tup.Value.type;

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
                        allowedSeeds.Add(id);
                    }
                    else if (isDeny)
                    {
                        deniedSeeds.Add(id);
                    }
                    break;
                case SObject.fertilizerCategory:
                    if (isAllow)
                    {
                        allowedFertilizers.Add(id);
                    }
                    else if (isDeny)
                    {
                        deniedFertilizers.Add(id);
                    }
                    break;
                default:
                    ModEntry.ModMonitor.Log($"{item} with {id} is type {type}, not a seed or fertilizer, skipping.");
                    break;
            }
        }
    }
}
