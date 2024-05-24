using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;
using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using DresserMiniMenu.Framework;
using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using StardewModdingAPI.Utilities;

using StardewValley.Menus;
using StardewValley.Objects;

using static StardewValley.Menus.CoopMenu;

namespace DresserMiniMenu.HarmonyPatches;

/// <summary>
/// Holds patches against ShopMenu to make minimenu a thing.
/// </summary>
[HarmonyPatch(typeof(ShopMenu))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class DresserMenuDoll
{
    private static readonly PerScreen<MiniFarmerMenu?> MiniMenu = new();
    private static readonly PerScreen<int> LastKeyboardTick = new(() => 0);

    #region delegates
    private static readonly Lazy<Func<ShopMenu, int>> _getCurrentTab = new(() =>
        typeof(ShopMenu).GetCachedField("currentTab", ReflectionCache.FlagTypes.InstanceFlags)
        .GetInstanceFieldGetter<ShopMenu, int>()
    );
    #endregion

    /// <summary>
    /// Checks to see whether or not this instance of a ShopMenu has an active MiniFarmerMenu associated with it.
    /// </summary>
    /// <param name="instance">Shop menu instance.</param>
    /// <returns>True if it has an active minifarmermenu, false otherwise.</returns>
    internal static bool IsActive(ShopMenu instance) => IsActive(instance, out _);

    /// <summary>
    /// Checks to see whether or not this instance of a ShopMenu has an active MiniFarmerMenu associated with it.
    /// </summary>
    /// <param name="instance">Shop menu instance.</param>
    /// <param name="current">The current minifarmermenu.</param>
    /// <returns>True if it has an active minifarmermenu, false otherwise.</returns>
    [MethodImpl(TKConstants.Hot)]
    internal static bool IsActive(ShopMenu instance, [NotNullWhen(true)] out MiniFarmerMenu? current)
    {
        if (MiniMenu.Value?.ShopMenu is not null && ReferenceEquals(instance, MiniMenu.Value?.ShopMenu))
        {
            current = MiniMenu.Value;
            return true;
        }
        current = null;
        return false;
    }

    #region setup

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopMenu.setUpStoreForContext))]
    private static void PostfixSetup(ShopMenu __instance)
    {
        try
        {
            if (!ModEntry.Config.DresserDressup)
            {
                MiniMenu.Value = null;
            }
            else if (__instance.storeContext == ShopMenuPatcher.DRESSER)
            {
                MiniMenu.Value = new(__instance, Game1.player);
                __instance.tabButtons.Add(new ClickableTextureComponent(
                    new Rectangle(0, 0, 64, 64),
                    AssetManager.Icons,
                    new Rectangle(53, 96, 16, 16),
                    4f)
                {
                    myID = 99999 + __instance.tabButtons.Count,
                    upNeighborID = -99998,
                    downNeighborID = -99998,
                    rightNeighborID = 3546,
                });
                __instance.repositionTabs();
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("setting up mini dresser menu", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopMenu.applyTab))]
    private static void Postfix(ShopMenu __instance)
    {
        try
        {
            if (__instance.storeContext == ShopMenuPatcher.DRESSER && _getCurrentTab.Value(__instance) == 6)
            {
                __instance.forSale.Clear();
                foreach (ISalable? item in __instance.itemPriceAndStock.Keys)
                {
                    if (item is not Item actual)
                    {
                        __instance.forSale.Add(item);
                        continue;
                    }
                    if (actual is Clothing)
                    {
                        // tabs 2 and 3
                        continue;
                    }
                    switch (actual.Category)
                    {
                        case SObject.hatCategory: // tab 1
                        case SObject.bootsCategory: // tab 4
                        case SObject.ringCategory: // tab 5
                            continue;
                        default:
                            __instance.forSale.Add(item);
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("Adjusting for misc tab", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopMenu.gameWindowSizeChanged))]
    private static void PostfixResize(ShopMenu __instance, Rectangle oldBounds, Rectangle newBounds)
    {
        if (IsActive(__instance, out MiniFarmerMenu? mini))
        {
            try
            {
                mini.gameWindowSizeChanged(oldBounds, newBounds);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError("changing window size for mini farmer menu", ex);
            }
        }
    }

    #endregion

    #region draw methods.

    private static void DrawMenu(ShopMenu __instance, SpriteBatch b)
    {
        if (IsActive(__instance, out MiniFarmerMenu? mini))
        {
            try
            {
                mini.draw(b);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError("drawing mini farmer menu", ex);
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ShopMenu.draw))]
    private static IEnumerable<CodeInstruction>? TranspileDraw(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                OpCodes.Ldarg_0,
                (OpCodes.Ldfld, typeof(ShopMenu).GetCachedField(nameof(ShopMenu.inventory), ReflectionCache.FlagTypes.InstanceFlags)),
                OpCodes.Ldarg_1,
                (OpCodes.Callvirt, typeof(IClickableMenu).GetCachedMethod<SpriteBatch>(nameof(IClickableMenu.draw), ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .Advance(4)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Call, typeof(DresserMenuDoll).GetCachedMethod(nameof(DrawMenu), ReflectionCache.FlagTypes.StaticFlags)),
            });

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }

    #endregion

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopMenu.performHoverAction))]
    private static void PostfixHover(ShopMenu __instance, int x, int y)
    {
        if (IsActive(__instance, out MiniFarmerMenu? mini))
        {
            try
            {
                mini.performHoverAction(x, y);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError("hovering for mini menu", ex);
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("cleanupBeforeExit")]
    private static void PrefixShutdown(ShopMenu __instance)
    {
        if (MiniMenu.Value?.ShopMenu is not null && ReferenceEquals(__instance, MiniMenu.Value.ShopMenu))
        {
            try
            {
                MiniMenu.Value.BeforeExit();
                MiniMenu.Value.exitThisMenu();
                MiniMenu.Value = null;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError("cleaning up smol menu", ex);
            }
        }
    }

    #region interaction

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopMenu.switchTab))]
    private static void PostfixSwitchTab(ShopMenu __instance)
    {
        try
        {
            if (IsActive(__instance, out MiniFarmerMenu? mini))
            {
                mini.ApplyFilter();
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adjusting filters", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopMenu.update))]
    private static void PostfixUpdate(ShopMenu __instance, GameTime time)
    {
        try
        {
            if (IsActive(__instance, out MiniFarmerMenu? mini))
            {
                mini.update(time);
                if (Game1.ticks > LastKeyboardTick.Value + 20)
                {
                    LastKeyboardTick.Value = Game1.ticks;
                    mini.UpdateForFilter();
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adjusting filters", ex);
        }
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    [HarmonyPatch(nameof(ShopMenu.receiveLeftClick))]
    private static bool PrefixRecieveClick(ShopMenu __instance, int x, int y, bool playSound)
    {
        try
        {
            if (IsActive(__instance, out MiniFarmerMenu? mini))
            {
                if (mini.TryClickFloatingElements(x, y, playSound))
                {
                    return false;
                }
                else if (mini.isWithinBounds(x, y))
                {
                    mini.receiveLeftClick(x, y, playSound);
                    return false;
                }
                else if (mini.ShopMenu?.heldItem is not null)
                {
                    if (mini.ShopMenu.IsWithinShopMenu(x, y)
                        && mini.ShopMenu.onSell(mini.ShopMenu.heldItem))
                    {
                        mini.ShopMenu.heldItem = null;
                        return false;
                    }
                }
                else if (mini.ShopMenu?.inventory.leftClick(x, y, null, playSound) is Item pickup)
                {
                    mini.ShopMenu.heldItem = pickup;
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("handling left click on smol menu", ex);
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    [HarmonyPatch(nameof(ShopMenu.receiveRightClick))]
    private static bool PrefixRecieveRightClick(ShopMenu __instance, int x, int y, bool playSound)
    {
        try
        {
            if (IsActive(__instance, out MiniFarmerMenu? mini))
            {
                if (mini.isWithinBounds(x, y))
                {
                    mini.receiveLeftClick(x, y, playSound);
                    return false;
                }
                else if (mini.ShopMenu?.heldItem is not null)
                {
                    if (mini.ShopMenu.IsWithinShopMenu(x, y)
                        && mini.ShopMenu.onSell(mini.ShopMenu.heldItem))
                    {
                        mini.ShopMenu.heldItem = null;
                        return false;
                    }
                }
                else if (mini.ShopMenu?.inventory.rightClick(x, y, null, playSound) is Item pickup)
                {
                    mini.ShopMenu.heldItem = pickup;
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("handling right click on smol menu", ex);
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    [HarmonyPatch(nameof(ShopMenu.receiveKeyPress))]
    private static bool PrefixKeyPress(ShopMenu __instance)
        => !IsActive(__instance, out MiniFarmerMenu? mini) || !mini.HasKeyboard;

    #endregion

    private static bool IsWithinShopMenu(this ShopMenu shop, int x, int y)
        => x >= shop.xPositionOnScreen && x < shop.xPositionOnScreen + shop.width
        && y >= shop.yPositionOnScreen && y < shop.yPositionOnScreen + shop.height - 220;
}
