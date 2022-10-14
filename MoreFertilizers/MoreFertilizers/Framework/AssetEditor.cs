using AtraCore;
using AtraCore.Models;

using AtraShared.ConstantsAndEnums;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley.GameData;

namespace MoreFertilizers.Framework;

/// <summary>
/// Handles asset editing for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1306:Field names should begin with lower-case letter", Justification = "Effective constants are all caps.")]
internal static class AssetEditor
{
    /// <summary>
    /// The mail key for the organic veggies reward.
    /// </summary>
    internal const string ORGANICVEGGIEMAIL = "atravita_OrganicCrops_Reward";

    /// <summary>
    /// Our special orders.
    /// </summary>
    private static readonly Lazy<Dictionary<string, SpecialOrderData>> SpecialOrders = new(() =>
    {
        Dictionary<string, SpecialOrderData> ret = new();
        int i = 0;
        foreach (string? filename in Directory.GetFiles(PathUtilities.NormalizePath(ModEntry.DIRPATH + "/assets/special-orders/"), "*.json"))
        {
            try
            {
                Dictionary<string, SpecialOrderData> orders = ModEntry.ModContentHelper.Load<Dictionary<string, SpecialOrderData>>(Path.GetRelativePath(ModEntry.DIRPATH, filename));
                foreach ((string key, SpecialOrderData order) in orders)
                {
                    ret[key] = order;
                    i++;
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"{filename} may not be valid:\n\n{ex}", LogLevel.Error);
            }
        }
        ModEntry.ModMonitor.Log($"Found {i} Special Orders");
        return ret;
    });

#pragma warning disable SA1310 // Field names should not contain underscore. Reviewed.
    private static IAssetName SPECIAL_ORDERS_LOCATION = null!;
    private static IAssetName SPECIAL_ORDERS_STRINGS = null!;
    private static IAssetName MAIL = null!;
    private static IAssetName LEWIS_DIALOGUE = null!;
#pragma warning restore SA1310 // Field names should not contain underscore

    #region minicache
    private static int lastTick = -1;
    private static bool seenBoat = false;

    private static bool HasSeenBoat
    {
        get
        {
            if ((Game1.ticks & ~0b11) != lastTick)
            {
                lastTick = Game1.ticks & ~0b11;
                seenBoat = Utility.doesAnyFarmerHaveOrWillReceiveMail("seenBoatJourney");
            }
            return seenBoat;
        }
    }
    #endregion

    /// <summary>
    /// Initializes the AssetEditor.
    /// </summary>
    /// <param name="parser">Game content helper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        SPECIAL_ORDERS_LOCATION = parser.ParseAssetName("Data/SpecialOrders");
        SPECIAL_ORDERS_STRINGS = parser.ParseAssetName("Strings/SpecialOrderStrings");
        MAIL = parser.ParseAssetName("Data/mail");
        LEWIS_DIALOGUE = parser.ParseAssetName("Characters/Dialogue/Lewis");
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
        else if (HasSeenBoat)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(SPECIAL_ORDERS_LOCATION))
            {
                e.Edit(EditSpecialOrdersImpl, AssetEditPriority.Early);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(SPECIAL_ORDERS_STRINGS))
            {
                e.Edit(EditSpecialOrdersStringsImpl, AssetEditPriority.Early);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(MAIL))
            {
                e.Edit(EditMailImpl, AssetEditPriority.Early);
            }
        }
    }

    /// <summary>
    /// Handles editing special order dialogue. This is seperate so it's only
    /// registed if necessary.
    /// </summary>
    /// <param name="e">event args.</param>
    internal static void EditSpecialOrderDialogue(AssetRequestedEventArgs e)
    {
        if (HasSeenBoat && e.NameWithoutLocale.IsEquivalentTo(LEWIS_DIALOGUE))
        {
            e.Edit(EditLewisDialogueImpl, AssetEditPriority.Early);
        }
    }

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

    private static void EditSpecialOrdersImpl(IAssetData asset)
    {
        IAssetDataForDictionary<string, SpecialOrderData>? editor = asset.AsDictionary<string, SpecialOrderData>();
        foreach ((string key, SpecialOrderData order) in SpecialOrders.Value)
        {
            editor.Data[key] = order;
        }
    }

    private static void EditSpecialOrdersStringsImpl(IAssetData asset)
    {
        IAssetDataForDictionary<string, string>? editor = asset.AsDictionary<string, string>();
        editor.Data["atravita.OrganicCrops.Name"] = I18n.Specialorder_Organic_Name();
        editor.Data["atravita.OrganicCrops.Text"] = I18n.Specialorder_Organic_Text();
        editor.Data["atravita.OrganicCrops.gather"] = I18n.Specialorder_Organic_Gather();
        editor.Data["atravita.OrganicCrops.ship"] = I18n.Specialorder_Organic_Ship();
    }

    private static void EditMailImpl(IAssetData asset)
    {
        IAssetDataForDictionary<string, string>? editor = asset.AsDictionary<string, string>();
        editor.Data[ORGANICVEGGIEMAIL] = $"@,^{I18n.Specialorder_Organic_Mail_Text()}^^   --{Game1.getCharacterFromName("Lewis")?.displayName ?? I18n.Lewis()}%item bigobject 272 %%[#]{I18n.Specialorder_Organic_Mail_Text()}";
    }

    private static void EditLewisDialogueImpl(IAssetData asset)
    {
        IAssetDataForDictionary<string, string>? editor = asset.AsDictionary<string, string>();
        editor.Data["atravita.OrganicCrops_InProgress"] = I18n.Specialorder_Organic_LewisInprogress();
    }
}