using StardewModdingAPI.Events;
using StardewValley.Menus;

namespace GingerIslandMainlandAdjustments.Utils;

/// <summary>
/// Handles Sandy's shop if she's away.
/// </summary>
internal static class SandyShop
{
    private static bool handlingshop = false;

    /// <summary>
    /// Handles running Sandy's shop if she' snot there.
    /// </summary>
    /// <param name="e">Button pressed event arguments.</param>
    public static void HandleShop(ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
        {
            return;
        }
        if (e.Button is SButton.MouseRight
            && Game1.currentLocation.Name.Equals("SandyHouse", StringComparison.OrdinalIgnoreCase)
            && e.Cursor.Tile.X == 2 && (e.Cursor.Tile.Y == 5 || e.Cursor.Tile.Y == 6))
        {
            GameLocation sandyHouse = Game1.currentLocation;
            Globals.ModMonitor.DebugLog("In Sandy's House");
            if (Game1.IsVisitingIslandToday("Sandy") && Game1.getCharacterFromName("Sandy")?.currentLocation?.Name != "SandyHouse")
            {
                IReflectedMethod? onSandyShop = Globals.ReflectionHelper.GetMethod(sandyHouse, "onSandyShopPurchase");
                IReflectedMethod? getSandyStock = Globals.ReflectionHelper.GetMethod(sandyHouse, "sandyShopStock");
                if (onSandyShop is not null && getSandyStock is not null && !handlingshop)
                {
                    handlingshop = true;
                    Game1.drawObjectDialogue(I18n.SandyAwayShopMessage());
                    Game1.afterDialogues = () =>
                    {
                        Game1.activeClickableMenu = new ShopMenu(
                                itemPriceAndStock: getSandyStock.Invoke<Dictionary<ISalable, int[]>>(),
                                currency: 0,
                                who: "Sandy",
                                on_purchase: (ISalable sellable, Farmer who, int amount) => onSandyShop.Invoke<bool>(sellable, who, amount));
                        handlingshop = false;
                    };
                }
            }
        }
    }
}