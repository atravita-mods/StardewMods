using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

using StardewValley.Extensions;
using StardewValley.GameData.Crops;
using StardewValley.Menus;
using StardewValley.Objects;

using static StardewValley.Menus.ShopMenu;

namespace ShopTabs.Framework;
internal static class ShopTabsManager
{
    private readonly record struct TabEntry(string textureName, Rectangle location, Func<ISalable, bool> filter, bool useBackground, string[]? excludedShops = null);

    private static readonly List<TabEntry> _tabs = [];
    private static readonly WeakReference<ShopMenu?> _lastEditedMenu = new(null);

    internal static void Init()
    {
        _tabs.Clear();

        // seasonal seeds
        _tabs.Add(
            new(
                Game1.objectSpriteSheetName,
                new Rectangle(240, 320, 16, 16),
                static item => IsCropOfSeason(item, Season.Spring),
                true
                ));
        _tabs.Add(
            new(
                Game1.objectSpriteSheetName,
                new Rectangle(256, 320, 16, 16),
                static item => IsCropOfSeason(item, Season.Summer),
                true));
        _tabs.Add(
        new(
            Game1.objectSpriteSheetName,
            new Rectangle(272, 320, 16, 16),
            static item => IsCropOfSeason(item, Season.Fall),
            true));
        _tabs.Add(
            new(
                Game1.objectSpriteSheetName,
                new Rectangle(288, 320, 16, 16),
                static item => IsCropOfSeason(item, Season.Winter),
                true));

        // saplings
        _tabs.Add(
            new(
                Game1.objectSpriteSheetName,
                new Rectangle(96, 416, 16, 16),
                static item => item is SObject obj && obj.IsFruitTreeSapling(),
                true
                ));

        // recipes
        _tabs.Add(
            new(
                Game1.objectSpriteSheetName,
                new Rectangle(144, 592, 16, 16),
                static item => item is SObject obj && obj.IsRecipe,
                true
                ));

        // wallpaper
        _tabs.Add(new(
            Game1.mouseCursors2Name,
            new Rectangle(48, 64, 16, 16),
            static item => item is Wallpaper,
            false,
            ["Dresser", Game1.shop_catalogue]));
    }

    /// <inheritdoc cref="IDisplayEvents.MenuChanged"/>
    internal static void OnShopMenu(MenuChangedEventArgs e)
    {
        if (Game1.activeClickableMenu is not ShopMenu menu
            || menu.ShopId == Game1.shop_adventurersGuildItemRecovery
            || (_lastEditedMenu.TryGetTarget(out ShopMenu? last) && ReferenceEquals(last, menu)))
        {
            return;
        }

        _lastEditedMenu.SetTarget(menu);

        bool hasSetUpFilters = true;
        if (menu.ShopId == Game1.shop_carpenter)
        {
            menu.UseFurnitureCatalogueTabs();
            for (int i = menu.tabButtons.Count - 1; i >= 1; i--)
            {
                var proposed_tab = menu.tabButtons[i];
                if (!menu.itemPriceAndStock.Keys.Any(item => proposed_tab.Filter(item)))
                {
                    menu.tabButtons.RemoveAt(i);
                }
            }
        }

        if (menu.tabButtons?.Count is null or 0)
        {
            hasSetUpFilters = false;
            ModEntry.ModMonitor.VerboseLog($"{menu.ShopId} raised with no tabs.");
            menu.tabButtons ??= [];
        }

        HashSet<(Texture2D, Rectangle)> addedTabs = [];
        foreach (ShopTabClickableTextureComponent? tab in menu.tabButtons)
        {
            addedTabs.Add((tab.texture, tab.sourceRect));
        }

        foreach (TabEntry tab in _tabs)
        {
            try
            {
                if (tab.excludedShops?.Contains(menu.ShopId) == true)
                {
                    continue;
                }
                if (!menu.itemPriceAndStock.Keys.Any(salable => tab.filter(salable)))
                {
                    continue;
                }

                Texture2D texture = Game1.content.Load<Texture2D>(tab.textureName);
                if (texture.IsDisposed)
                {
                    ModEntry.ModMonitor.Log($"Texture {tab.textureName} is disposed. Forcing refresh.", LogLevel.Warn);
                    ModEntry.GameContentHelper.InvalidateCacheAndLocalized(tab.textureName);
                    continue;
                }

                if (addedTabs.Add((texture, tab.location)))
                {
                    if (!hasSetUpFilters)
                    {
                        menu.tabButtons.Add(new ShopTabClickableTextureComponent(new Rectangle(0, 0, 64, 64), Game1.mouseCursors2, new Rectangle(96, 48, 16, 16), Game1.pixelZoom)
                        {
                            myID = region_tabStartIndex + menu.tabButtons.Count,
                            upNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                            downNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                            rightNeighborID = region_shopButtonModifier,
                            Filter = static (ISalable item) => true,
                        });
                        hasSetUpFilters = true;
                    }

                    if (tab.useBackground)
                    {
                        menu.tabButtons.Add(new ItemShopMenuTab(
                            texture,
                            tab.location)
                        {
                            myID = region_tabStartIndex + menu.tabButtons.Count,
                            upNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                            downNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                            rightNeighborID = region_shopButtonModifier,
                            Filter = tab.filter,
                        });
                    }
                    else
                    {
                        menu.tabButtons.Add(new ShopTabClickableTextureComponent(
                            new Rectangle(0, 0, 64, 64),
                            texture,
                            tab.location,
                            Game1.pixelZoom)
                        {
                            myID = region_tabStartIndex + menu.tabButtons.Count,
                            upNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                            downNeighborID = ClickableComponent.SNAP_AUTOMATIC,
                            rightNeighborID = region_shopButtonModifier,
                            Filter = tab.filter,
                        });
                    }
                }
            }
            catch (ContentLoadException ex)
            {
                ModEntry.ModMonitor.Log($"Failed to load texture {tab.textureName}, skipping.", LogLevel.Warn);
                ModEntry.ModMonitor.Log(ex.ToString());
            }
            catch (Exception exception)
            {
                ModEntry.ModMonitor.LogError("adding tab to menu", exception);
            }
        }

        if (menu.tabButtons.Count == 1)
        {
            menu.tabButtons.Clear();
        }
        menu.repositionTabs();
        if (menu.tabButtons.Count > 0)
        {
            foreach (ClickableComponent forSaleButton in menu.forSaleButtons)
            {
                forSaleButton.leftNeighborID = ClickableComponent.SNAP_AUTOMATIC;
            }
        }
    }

    private static bool IsCropOfSeason(ISalable item, Season season)
    => item.HasTypeObject()
        && item is SObject crop
        && crop.Category == SObject.SeedsCategory
        && Game1.cropData.TryGetValue(crop.ItemId, out CropData? cropData)
        && cropData.Seasons.Contains(season);
}