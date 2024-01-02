using AtraBase.Collections;

using AtraCore;
using AtraCore.Framework.Models;

using AtraShared.Caching;
using AtraShared.ConstantsAndEnums;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;

using StardewModdingAPI.Events;

namespace MoreFertilizers.Framework;

/// <summary>
/// Handles asset editing for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Preference.")]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1306:Field names should begin with lower-case letter", Justification = "Effective constants are all caps.")]
internal static class AssetEditor
{
    /// <summary>
    /// The mail key for the organic veggies reward.
    /// </summary>
    internal const string ORGANICVEGGIEMAIL = "atravita_OrganicCrops_Reward";

    /// <summary>
    /// The letter key used for the bountiful bush fertilizer's unlock.
    /// </summary>
    internal const string BOUNTIFUL_BUSH_UNLOCK = "atravita_Bountiful_Bush";

    /// <summary>
    /// A letter used to tell the player that robin now sells fertilizer after George's leek special order.
    /// </summary>
    internal const string GEORGE_EVENT = "atravita_George_Letter";

    private static readonly TickCache<bool> HasSeenBoat = new(static () => FarmerHelpers.HasAnyFarmerRecievedFlag("seenBoatJourney"));

    private static IAssetName LEWIS_DIALOGUE = null!;
    private static IAssetName RADIOACTIVE_DENYLIST = null!;

    private static HashSet<string>? denylist = null;

    /// <summary>
    /// Initializes the AssetEditor.
    /// </summary>
    /// <param name="parser">Game content helper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        LEWIS_DIALOGUE = parser.ParseAssetName("Characters/Dialogue/Lewis");
        RADIOACTIVE_DENYLIST = parser.ParseAssetName("Mods/atravita/MoreFertilizers/RadioactiveDenylist");
    }

    /// <summary>
    /// Handles asset editing.
    /// </summary>
    /// <param name="e">Asset requested event arguments.</param>
    internal static void Edit(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(AtraCoreConstants.PrismaticMaskData))
        {
            e.Edit(EditPrismaticMasks);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(RADIOACTIVE_DENYLIST))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, string>, AssetLoadPriority.Exclusive);
        }
    }

    /// <inheritdoc cref="IContentEvents.AssetsInvalidated"/>
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (assets is null || assets.Contains(RADIOACTIVE_DENYLIST))
        {
            denylist = null;
        }
    }

    /// <summary>
    /// Gets a hashset of ids that should be excluded from the radioactive fertilizer.
    /// </summary>
    /// <returns>ids to exclude.</returns>
    internal static HashSet<string> GetRadioactiveExclusions()
    {
        if (denylist is not null)
        {
            return denylist;
        }

        ModEntry.ModMonitor.DebugOnlyLog("Resolving radioactive fertilizer denylist", LogLevel.Info);

        HashSet<string> ret = [];
        foreach (string item in Game1.content.Load<Dictionary<string, string>>(RADIOACTIVE_DENYLIST.BaseName).Keys)
        {
            string? id = MFUtilities.ResolveID(item);
            if (id is not null)
            {
                ret.Add(id);
            }
        }

        denylist = ret;
        return denylist;
    }

    #region editors

    private static void EditPrismaticMasks(IAssetData asset)
    {
        IAssetDataForDictionary<string, DrawPrismaticModel>? editor = asset.AsDictionary<string, DrawPrismaticModel>();

        DrawPrismaticModel? prismatic = new()
        {
            ItemType = ItemTypeEnum.SObject,
            Identifier = "Prismatic Fertilizer - More Fertilizers",
        };

        if (!editor.Data.TryAdd(prismatic.Identifier, prismatic))
        {
            ModEntry.ModMonitor.Log("Could not add prismatic fertilizer to DrawPrismatic", LogLevel.Warn);
        }
    }

    #endregion
}