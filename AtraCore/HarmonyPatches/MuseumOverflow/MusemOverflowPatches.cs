using System.Runtime.InteropServices;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Utilities;

using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.Menus;

namespace AtraCore.HarmonyPatches.MuseumOverflow;

/// <summary>
/// Holds patches to make museum overflow work.
/// </summary>
[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class MusemOverflowPatches
{
    /// <summary>
    /// The key for the museum inventory.
    /// </summary>
    private const string INVENTORY_NAME = "atravita.AtraCore.MuseumOverflow";

    private static readonly PerScreen<MuseumOverflowMenu?> menu = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MuseumMenu), MethodType.Constructor, new[] { typeof(InventoryMenu.highlightThisItem) } )]
    private static void PostfixMuseumConstruction(MuseumMenu __instance)
    {
        menu.Value = new(__instance, Game1.player.team.GetOrCreateGlobalInventory(INVENTORY_NAME));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MuseumMenu), nameof(InventoryMenu.draw))]
    private static void PrefixDraw(SpriteBatch b)
    {
        if (menu.Value is { } m)
        {
            m.draw(b);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MuseumMenu), "cleanupBeforeExit")]
    private static void PostfixCleanup()
    {
        menu.Value = null;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MuseumMenu), nameof(MuseumMenu.gameWindowSizeChanged))]
    private static void PostfixWindowSizeChange(Rectangle oldBounds, Rectangle newBounds)
    {
        if (menu.Value is { } m)
        {
            m.gameWindowSizeChanged(oldBounds, newBounds);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MuseumMenu), nameof(MuseumMenu.update))]
    private static void PostfixUpdate(GameTime time)
    {
        if (menu.Value is { } m)
        {
            m.update(time);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MuseumMenu), nameof(MuseumMenu.receiveLeftClick))]
    private static void PostfixLeftClick(int x, int y, bool playSound)
    {
        if (menu.Value is { } m)
        {
            m.receiveLeftClick(x, y, playSound);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MenuWithInventory), nameof(MenuWithInventory.performHoverAction))]
    private static void PostfixHoverAction(MenuWithInventory __instance, int x, int y)
    {
        if (__instance is MuseumMenu && menu.Value is { } m)
        {
            m.performHoverAction(x, y);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LibraryMuseum), nameof(LibraryMuseum.GetDonatedByContextTag))]
    private static void PostfixMuseumItems(Dictionary<string, int> __result)
    {
        try
        {
            Inventory storage = Game1.player.team.GetOrCreateGlobalInventory(INVENTORY_NAME);
            storage.RemoveEmptySlots();

            if (storage.Count == 0)
            {
                return;
            }

            ref int prev = ref CollectionsMarshal.GetValueRefOrAddDefault(__result, string.Empty, out _);
            prev += storage.Count;

            foreach (Item? item in storage)
            {
                foreach (string? tag in ItemContextTagManager.GetBaseContextTags(item.ItemId))
                {
                    if (__result.TryGetValue(tag, out var value))
                    {
                        __result[tag] = value + 1;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding storage items to museum", ex);
        }
    }
}
