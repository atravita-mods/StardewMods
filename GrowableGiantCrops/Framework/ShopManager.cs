using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AtraShared.Caching;
using AtraShared.Utils;

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

    private static IAssetName robinHouse = null!;
    private static IAssetName witchHouse = null!;
    private static IAssetName mail = null!;

    /// <summary>
    /// Initializes the asset names.
    /// </summary>
    /// <param name="parser">Game Content Helper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        robinHouse = parser.ParseAssetName("Maps/ScienceHouse");
        witchHouse = parser.ParseAssetName("Maps/WitchHut");
        mail = parser.ParseAssetName("Data/mail");
    }

    private static void PopulateSellablesWithResourceClumps(this Dictionary<ISalable, int[]> sellables)
    {
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
}
