using System.Diagnostics;

using AtraBase.Models.Result;
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
using StardewModdingAPI.Utilities;

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

    // giant crop shop state.
    private static readonly PerScreen<Dictionary<int, int>?> Stock = new();
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
        if (e.NameWithoutLocale.IsEquivalentTo(mail))
        {
            e.Edit(static (asset) =>
            {
                IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;

                data[RESOURCE_SHOP_NAME] = I18n.RobinMail();
                data[GIANT_CROP_SHOP_NAME] = I18n.WitchMail();
            });
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(robinHouse))
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

            ShopMenu shop = new(sellables, who: "Robin") { storeContext = RESOURCE_SHOP_NAME };
            if (NPCCache.GetByVillagerName("Robin") is NPC robin)
            {
                shop.portraitPerson = robin;
            }
            shop.potraitPersonDialogue = stringUtils.ParseAndWrapText(I18n.ShopMessage_Robin(), Game1.dialogueFont, 304);
            Game1.activeClickableMenu = shop;
        }
        else if (Game1.currentLocation.Name == "WitchHut"
            && Game1.currentLocation.doesTileHaveProperty((int)e.Cursor.GrabTile.X, (int)e.Cursor.GrabTile.Y, "Action", BUILDING) == GIANT_CROP_SHOP_NAME)
        {
            input.SurpressClickInput();

            Dictionary<ISalable, int[]> sellables = new();
            sellables.PopulateWitchShop();

            Game1.activeClickableMenu = new ShopMenu(sellables, on_purchase: TrackStock) { storeContext = GIANT_CROP_SHOP_NAME };
        }
    }

    /// <inheritdoc cref="IGameLoopEvents.DayEnding"/>
    /// <remarks>Used to reset the shop inventory and send mail about the shops.</remarks>
    internal static void OnDayEnd()
    {
        Stock.Value = null;

        // add Robin letter for tomorrow.
        if (Game1.player.getFriendshipLevelForNPC("Robin") > 250
            && !Game1.player.mailReceived.Contains(RESOURCE_SHOP_NAME)
            && Game1.player.mailReceived.Contains("robinKitchenLetter"))
        {
            Game1.addMailForTomorrow(mailName: RESOURCE_SHOP_NAME);
        }

        // add Witch letter for tomorrow.
        if (Game1.player.hasMagicInk && !Game1.player.mailReceived.Contains(GIANT_CROP_SHOP_NAME))
        {
            Game1.addMailForTomorrow(mailName: GIANT_CROP_SHOP_NAME);
        }
    }

    private static bool TrackStock(ISalable salable, Farmer farmer, int count)
    {
        if (salable is InventoryGiantCrop crop && Stock.Value?.TryGetValue(crop.ParentSheetIndex, out int remaining) == true)
        {
            remaining -= count;
            if (remaining <= 0)
            {
                Stock.Value.Remove(crop.ParentSheetIndex);
            }
            else
            {
                Stock.Value[crop.ParentSheetIndex] = remaining;
            }
        }
        return false; // do not want to yeet the menu.
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

        if (!Game1.player.Items.Any(item => item is ShovelTool))
        {
            sellables.TryAdd(new ShovelTool(), new[] { 3_000, 1 });
        }

        if (PerfectFaarm.GetValue())
        {
            foreach (int idx in ModEntry.YieldAllGiantCropIndexes())
            {
                int price = GetPriceOfProduct(idx) ?? 0;
                _ = sellables.TryAdd(new InventoryGiantCrop(idx, 1), new int[] { Math.Max(price * 30, 5_000), ShopMenu.infiniteStock});
            }
        }
        else
        {
            Stock.Value ??= GenerateDailyStock();
            if (Stock.Value is null)
            {
                return;
            }

            foreach ((int index, int count) in Stock.Value)
            {
                if (count <= 0)
                {
                    continue;
                }

                int price = GetPriceOfProduct(index) ?? 0;
                _ = sellables.TryAdd(new InventoryGiantCrop(index, count), new int[] { Math.Max(price * 30, 5_000), count });
            }
        }
    }

    private static WeightedManager<int> GetWeightedManager()
    {
        WeightedManager<int> manager = new();

        foreach (int idx in ModEntry.YieldAllGiantCropIndexes())
        {
            int? price = GetPriceOfProduct(idx);
            if (price is not null)
            {
                manager.Add(new(2500d / Math.Clamp(price.Value, 50, 2500), idx));
            }
        }
        ModEntry.ModMonitor.DebugOnlyLog($"Got {manager.Count} giant crop entries for shop.");
        return manager;
    }

    private static Dictionary<int, int>? GenerateDailyStock()
    {
        weighted ??= GetWeightedManager();
        if (weighted.Count == 0)
        {
            return null;
        }

        Dictionary<int, int> chosen = new();

        int totalCount = Math.Max(5, ModEntry.GetTotalValidIndexes() / 5);
        for (int i = 0; i < totalCount; i++)
        {
            Option<int> picked = weighted.GetValue();
            if (picked.IsNone)
            {
                continue;
            }

            int idx = picked.Unwrap();
            if (!chosen.TryGetValue(idx, out int prev))
            {
                prev = 0;
            }
            chosen[idx] = prev + 5;
        }

        return chosen;
    }

    private static int? GetPriceOfProduct(int idx)
    => Game1Wrappers.ObjectInfo.TryGetValue(idx, out string? info) &&
       int.TryParse(info.GetNthChunk('/', SObject.objectInfoPriceIndex), out int price)
       ? price
       : null;
}
