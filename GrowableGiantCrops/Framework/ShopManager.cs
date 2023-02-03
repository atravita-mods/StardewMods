using System.Diagnostics;

using AtraBase.Models.WeightedRandom;

using AtraCore.Framework.Caches;

using AtraShared.Caching;
using AtraShared.Menuing;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;

using StardewModdingAPI.Events;

using StardewValley.Menus;

using xTile.Dimensions;
using xTile.ObjectModel;

using XTile = xTile.Tiles.Tile;

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

    private static WeightedManager<int>? weighted;

    private static IAssetName robinHouse = null!;
    private static IAssetName witchHouse = null!;

    private static IAssetName mail = null!;
    private static IAssetName dataObjectInfo = null!;

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
    }

    internal static void OnAssetInvalidated(AssetsInvalidatedEventArgs e)
    {
        weighted = null;
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
                static (asset) =>
                {
                    IAssetDataForMap? map = asset.AsMap();
                    XTile? tile = map.Data.GetLayer(BUILDING).PickTile(new Location((int)ModEntry.Config.ResourceShopLocation.X * 64, (int)ModEntry.Config.ResourceShopLocation.Y * 64), Game1.viewport.Size);
                    if (tile is null)
                    {
                        ModEntry.ModMonitor.Log($"Tile could not be edited for shop, please let atra know!", LogLevel.Warn);
                        return;
                    }
                    tile.Properties["Action"] = new PropertyValue(RESOURCE_SHOP_NAME);
                },
                AssetEditPriority.Default + 10);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(witchHouse))
        {
            e.Edit(
                static (asset) =>
                {
                    IAssetDataForMap? map = asset.AsMap();
                    XTile? tile = map.Data.GetLayer(BUILDING).PickTile(new Location((int)ModEntry.Config.GiantCropShopLocation.X * 64, (int)ModEntry.Config.GiantCropShopLocation.Y * 64), Game1.viewport.Size);
                    if (tile is null)
                    {
                        ModEntry.ModMonitor.Log($"Tile could not be edited for shop, please let atra know!", LogLevel.Warn);
                        return;
                    }
                    tile.Properties["Action"] = new PropertyValue(GIANT_CROP_SHOP_NAME);
                },
                AssetEditPriority.Default + 10);
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
            shop.potraitPersonDialogue = I18n.ShopMessage_Robin();
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

            var clumpItem = new InventoryResourceClump(clump, 1);
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
        return manager;
    }
}
