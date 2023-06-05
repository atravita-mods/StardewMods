using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Utilities;

using StardewValley.Menus;
using StopRugRemoval.Framework.Menus.MiniFarmerMenu;

namespace StopRugRemoval.HarmonyPatches;

/// <summary>
/// Holds patches against ShopMenu to make minimenu a thing.
/// </summary>
[HarmonyPatch(typeof(ShopMenu))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class DresserMenuDoll
{
    private static readonly PerScreen<MiniFarmerMenu?> _miniMenu = new();

    [MethodImpl(TKConstants.Hot)]
    private static bool IsActive(ShopMenu instance, [NotNullWhen(true)] out MiniFarmerMenu? current)
    {
        if (_miniMenu.Value?.ShopMenu is not null && ReferenceEquals(instance, _miniMenu.Value?.ShopMenu))
        {
            current = _miniMenu.Value;
            return true;
        }
        current = null;
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopMenu.setUpStoreForContext))]
    private static void PostfixSetup(ShopMenu __instance)
    {
        try
        {
            if (!ModEntry.Config.DresserDressup)
            {
                _miniMenu.Value = null;
            }
            else if (__instance.storeContext == "Dresser")
            {
                _miniMenu.Value = new(__instance);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("setting up mini dresser menu", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopMenu.draw))]
    private static void PostfixDraw(ShopMenu __instance, SpriteBatch b)
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
        if (_miniMenu.Value?.ShopMenu is not null && ReferenceEquals(__instance, _miniMenu.Value.ShopMenu))
        {
            try
            {
                _miniMenu.Value.exitThisMenu();
                _miniMenu.Value = null;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError("cleaning up smol menu", ex);
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    private static bool PrefixRecieveClick(ShopMenu __instance, int x, int y, bool playSound)
    {
        if (IsActive(__instance, out MiniFarmerMenu? mini) && mini.isWithinBounds(x, y))
        {
            mini.receiveLeftClick(x, y, playSound);
            return false;
        }
        return true;
    }
}
