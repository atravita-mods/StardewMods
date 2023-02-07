using System.Diagnostics;

using AtraBase.Models.WeightedRandom;
using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.Caches;

using AtraShared.Caching;
using AtraShared.Menuing;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;
using AtraShared.Wrappers;

using GrowableGiantCrops.Framework.InventoryModels;

using StardewModdingAPI.Events;

using StardewValley.Menus;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// Manages shops for this mod.
/// </summary>
internal static class ShopManager
{
    private const string BUILDING = "Buildings";
    private const string RESOURCE_SHOP_NAME = "atravita.ResourceShop";
    private const string GIANT_CROP_SHOP_NAME = "atravita.GiantCropShop";

    private static readonly TickCache<bool> HasReachedSkullCavern = new(() => FarmerHelpers.HasAnyFarmerRecievedFlag("qiChallengeComplete"));
    private static readonly TickCache<bool> PerfectFaarm = new(() => FarmerHelpers.HasAnyFarmerRecievedFlag("Farm_Eternal"));

    private static WeightedManager<int>? weighted;

    private static IAssetName robinHouse = null!;
    private static IAssetName witchHouse = null!;

    private static IAssetName mail = null!;
    private static IAssetName dataObjectInfo = null!;

    private static StringUtils stringUtils = null!;

    /// <summary>
    /// Initializes the asset names.
    /// </summary>
    /// <param name="parser">Game Content Helper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        robinHouse = parser.ParseAssetName("Maps/ScienceHouse");
        witchHouse = parser.ParseAssetName("Maps/WitchHut");
        mail = parser.ParseAssetName("Data/mail");
        dataObjectInfo = parser.ParseAssetName("Data/ObjectInformation");

        stringUtils = new(ModEntry.ModMonitor);
    }

    /// <inheritdoc cref="IContentEvents.AssetsInvalidated"/>
    internal static void OnAssetInvalidated(IReadOnlySet<IAssetName>? assets)
    {
        if (assets is null || assets.Contains(dataObjectInfo))
        {
            weighted = null;
        }
    }

    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    internal static void OnAssetRequested(AssetRequestedEventArgs e)
    {
        /* if (e.NameWithoutLocale.IsEquivalentTo(mail))
        {
            e.Edit(static (asset) =>
            {
                asset.AsDictionary<string, string>().Data[SHOPNAME] = I18n.Caroline_Mail();
            });
        }
        else */if (e.NameWithoutLocale.IsEquivalentTo(robinHouse))
        {
            e.Edit(
                apply: static (asset) => asset.AsMap().AddTileProperty(
                    monitor: ModEntry.ModMonitor,
                    layer: BUILDING,
                    key: "Action",
                    property: RESOURCE_SHOP_NAME,
                    placementTile: ModEntry.Config.ResourceShopLocation),
                priority: AssetEditPriority.Default + 10);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(witchHouse))
        {
            e.Edit(
                apply: static (asset) => asset.AsMap().AddTileProperty(
                    monitor: ModEntry.ModMonitor,
                    layer: BUILDING,
                    key: "Action",
                    property: GIANT_CROP_SHOP_NAME,
                    placementTile: ModEntry.Config.GiantCropShopLocation),
                priority: AssetEditPriority.Default + 10);
        }
    }

    /// <inheritdoc cref="IInputEvents.ButtonPressed"/>
    internal static void OnButtonPressed(ButtonPressedEventArgs e, IInputHelper input)
    {
        if ((!e.Button.IsActionButton() && !e.Button.IsUseToolButton())
            || !MenuingExtensions.IsNormalGameplay())
        {
            return;
        }

        if (Game1.currentLocation.Name == "ScienceHouse"
            && Game1.currentLocation.doesTileHaveProperty((int)e.Cursor.GrabTile.X, (int)e.Cursor.GrabTile.Y, "Action", BUILDING) == RESOURCE_SHOP_NAME)
        {
            input.SurpressClickInput();

            Dictionary<ISalable, int[]> sellables = new(ResourceClumpIndexesExtensions.Length);
            sellables.PopulateSellablesWithResourceClumps();

            ShopMenu shop = new(sellables, who: "Caroline") { storeContext = RESOURCE_SHOP_NAME };
            if (NPCCache.GetByVillagerName("Robin") is NPC robin)
            {
                shop.portraitPerson = robin;
            }
            shop.potraitPersonDialogue = stringUtils.ParseAndWrapText(I18n.ShopMessage_Robin(), Game1.dialogueFont, 304);
            Game1.activeClickableMenu = shop;
        }
    }

    private static void PopulateSellablesWithResourceClumps(this Dictionary<ISalable, int[]> sellables)
    {
        Debug.Assert(sellables is not null, "Sellables cannot be null.");

        foreach (ResourceClumpIndexes clump in ResourceClumpIndexesExtensions.GetValues())
        {
            int[] sellData;
            if (clump == ResourceClumpIndexes.Invalid)
            {
                continue;
            }
            else if (clump == ResourceClumpIndexes.Meteorite)
            {
                if (HasReachedSkullCavern.GetValue())
                {
                    sellData = new[] { 10_000, ShopMenu.infiniteStock };
                }
                else
                {
                    continue;
                }
            }
            else
            {
                sellData = new[] { 7_500, ShopMenu.infiniteStock };
            }

            InventoryResourceClump clumpItem = new InventoryResourceClump(clump, 1);
            _ = sellables.TryAdd(clumpItem, sellData);
        }
    }

    private static void PopulateWitchShop(this IDictionary<ISalable, int[]> sellables)
    {
        Debug.Assert(sellables is not null, "Sellables cannot be null.");
    }

    private static WeightedManager<int> GetWeightedManager()
    {
        WeightedManager<int> manager = new();

        foreach (int idx in ModEntry.JACropIds.Concat(ModEntry.MoreGiantCropsIds).Concat(new[] { 190, 254, 276 }))
        {
            int? price = GetPriceOfProduct(idx);
            if (price is not null)
            {
                manager.Add(new(2500d / Math.Clamp(price.Value, 1, 2500), idx));
            }
        }
        ModEntry.ModMonitor.DebugOnlyLog($"Got {manager.Count} giant crop entries for shop.");
        return manager;
    }

    private static int? GetPriceOfProduct(int idx)
    => Game1Wrappers.ObjectInfo.TryGetValue(idx, out string? info) &&
       int.TryParse(info.GetNthChunk('/', SObject.objectInfoPriceIndex), out int price)
       ? price
       : null;
}
