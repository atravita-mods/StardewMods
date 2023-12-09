using AtraShared.Utils.Extensions;

using StardewValley.GameData.Shops;
using StardewValley.Internal;

namespace CatGiftsRedux.Framework.Pickers;

/// <summary>
/// Picks a random unlocked hat.
/// </summary>
internal static class HatPicker
{
    /// <summary>
    /// Picks a random unlocked hat.
    /// </summary>
    /// <param name="random">Random instance.</param>
    /// <returns>Hat.</returns>
    internal static SObject? Pick(Random random)
    {
        ModEntry.ModMonitor.DebugOnlyLog("Picked hats");

        if (!DataLoader.Shops(Game1.content).TryGetValue("HatMouse", out ShopData? shop))
        {
            return null;
        }

        Dictionary<ISalable, ItemStockInformation> stock = ShopBuilder.GetShopStock("HatMouse", shop);
        return stock.Count != 0 ? stock.ElementAt(random.Next(stock.Count)).Key as SObject : null;
    }
}
