using AtraBase.Collections;

using StardewModdingAPI.Events;

namespace LastDayToPlantRedux.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Reviewed.")]
internal static class AssetManager
{
    #region denylist and allowlist
    private static readonly HashSet<int> AllowedFertilizersValue = new();
    private static readonly HashSet<int> DeniedFertilizersValue = new();
    private static readonly HashSet<int> AllowedSeedsValue = new();
    private static readonly HashSet<int> DeniedSeedsValue = new();
    private static bool accessProcessed = false;

    /// <summary>
    /// The mailflag used for this mod.
    /// </summary>
    internal static readonly string MailFlag = "atravita_LastDayLetter";

    /// <summary>
    /// Gets fertilizers that should always be allowed.
    /// </summary>
    internal static HashSet<int> AllowedFertilizers
    {
        get
        {
            ProcessAccessLists();
            return AllowedFertilizersValue;
        }
    }

    /// <summary>
    /// Gets fertilizers that should always be hidden.
    /// </summary>
    internal static HashSet<int> DeniedFertilizers
    {
        get
        {
            ProcessAccessLists();
            return DeniedFertilizersValue;
        }
    }

    /// <summary>
    /// Gets seeds that should always be allowed.
    /// </summary>
    internal static HashSet<int> AllowedSeeds
    {
        get
        {
            ProcessAccessLists();
            return AllowedSeedsValue;
        }
    }

    /// <summary>
    /// Gets seeds that should always be hidden.
    /// </summary>
    internal static HashSet<int> DeniedSeeds
    {
        get
        {
            ProcessAccessLists();
            return DeniedSeedsValue;
        }
    }

    #endregion

    /// <summary>
    /// The current mail for the player.
    /// </summary>
    private static string Message = string.Empty;

    /// <summary>
    /// The location of our access identifier->access dictionary.
    /// </summary>
    private static IAssetName accessLists = null!;

    /// <summary>
    /// The data asset for objects.
    /// </summary>
    private static IAssetName objectInfoName = null!;

    /// <summary>
    /// Gets the data asset for mail.
    /// </summary>
    internal static IAssetName DataMail { get; private set; } = null!;

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
        DataMail = parser.ParseAssetName("Data/mail");
        CropName = parser.ParseAssetName("Data/Crops");
        objectInfoName = parser.ParseAssetName("Data/ObjectInformation");
        accessLists = parser.ParseAssetName("Mods/atravita.LastDayToPlantRedux/AccessControl");
    }

    /// <summary>
    /// Updates mail on the start of a new day.
    /// </summary>
    /// <returns>True if there's crops with their last day today.</returns>
    internal static bool UpdateOnDayStart()
    {
        (string message, bool showplayer) = CropAndFertilizerManager.GenerateMessageString();
        Message = message;
        return showplayer;
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
        else if (e.NameWithoutLocale.IsEquivalentTo(DataMail) && !string.IsNullOrWhiteSpace(Message))
        {
            e.Edit(
            static (asset) =>
            {
                IDictionary<string, string>? data = asset.AsDictionary<string, string>().Data;
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

        AllowedFertilizersValue.Clear();
        DeniedFertilizersValue.Clear();
        AllowedSeedsValue.Clear();
        DeniedSeedsValue.Clear();

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
                        AllowedSeedsValue.Add(id);
                    }
                    else if (isDeny)
                    {
                        DeniedSeedsValue.Add(id);
                    }
                    break;
                case SObject.fertilizerCategory:
                    if (isAllow)
                    {
                        AllowedFertilizersValue.Add(id);
                    }
                    else if (isDeny)
                    {
                        DeniedFertilizersValue.Add(id);
                    }
                    break;
                default:
                    ModEntry.ModMonitor.Log($"{item} with {id} is type {type}, not a seed or fertilizer, skipping.");
                    break;
            }
        }
    }
}
