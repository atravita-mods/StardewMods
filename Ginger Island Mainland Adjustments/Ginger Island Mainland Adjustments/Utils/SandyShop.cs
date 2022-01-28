using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley.Menus;

namespace GingerIslandMainlandAdjustments.Utils;

/// <summary>
/// Handles Sandy's shop if she's away.
/// </summary>
internal static class SandyShop
{
    private static readonly PerScreen<bool> Handlingshop = new(createNewState: () => false);

    /// <summary>
    /// Handles running Sandy's shop if she's not there.
    /// </summary>
    /// <param name="e">Button pressed event arguments.</param>
    public static void HandleShop(ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
        {
            return;
        }
        if (e.Button.IsActionButton() // fix logic here - if player is facing the BuySandyShop tile should also work.
            && Game1.currentLocation.Name.Equals("SandyHouse", StringComparison.OrdinalIgnoreCase))
        {
            GameLocation sandyHouse = Game1.currentLocation;
            if (Utils.YieldSurroundingTiles(Game1.player.getTileLocation()).All((Point v) => sandyHouse.doesTileHaveProperty(v.X, v.Y, "Action", "Buildings")?.Contains("Buy") == true))
            {
                return;
            }
            if (Game1.IsVisitingIslandToday("Sandy") && sandyHouse.getCharacterFromName("Sandy") is null)
            {
                Game1.player.FacingDirection = Game1.up; // maybe? Check this.
                IReflectedMethod? onSandyShop = Globals.ReflectionHelper.GetMethod(sandyHouse, "onSandyShopPurchase");
                IReflectedMethod? getSandyStock = Globals.ReflectionHelper.GetMethod(sandyHouse, "sandyShopStock");
                if (onSandyShop is not null && getSandyStock is not null && !Handlingshop.Value)
                {
                    Handlingshop.Value = true; // Do not want to intercept any more clicks until shop menu is finished.
                    Game1.drawObjectDialogue(I18n.SandyAwayShopMessage());
                    Game1.afterDialogues = () =>
                    {
                        Game1.activeClickableMenu = new ShopMenu(
                                itemPriceAndStock: getSandyStock.Invoke<Dictionary<ISalable, int[]>>(),
                                currency: 0,
                                who: "Sandy",
                                on_purchase: (ISalable sellable, Farmer who, int amount) => onSandyShop.Invoke<bool>(sellable, who, amount));
                        Handlingshop.Value = false;
                    };
                }
            }
        }
    }

    /// <summary>
    /// Handles adding a box to Sandy's shop if she's gone.
    /// </summary>
    /// <param name="e">On Warped event arguments.</param>
    public static void AddBoxToShop(WarpedEventArgs e)
    {
        if (e.NewLocation.Name.Equals("SandyHouse", StringComparison.OrdinalIgnoreCase) && e.NewLocation.getCharacterFromName("Sandy") is null && Game1.IsVisitingIslandToday("Sandy"))
        {
            Vector2 tile = new(2f, 6f);
            foreach (Vector2 v in Utils.YieldAllTiles(e.NewLocation))
            {
                if (e.NewLocation.doesTileHaveProperty((int)v.X, (int)v.Y, "Action", "Buildings")?.Contains("Buy") == true)
                {
                    tile = v;
                    break;
                }
            }

            e.NewLocation.temporarySprites.Add(new TemporaryAnimatedSprite
            {
                texture = Game1.mouseCursors2,
                sourceRect = new Rectangle(129, 210, 13, 16),
                animationLength = 1,
                sourceRectStartingPos = new Vector2(129f, 210f),
                interval = 50000f,
                totalNumberOfLoops = 9999,
                position = (new Vector2(tile.X, tile.Y - 1) * 64f) + (new Vector2(3f, 0f) * 4f),
                scale = 4f,
                layerDepth = (((tile.Y * 64f) - 32f) / 10000f) + 0.01f,
                id = 777f,
            });
        }
    }
}