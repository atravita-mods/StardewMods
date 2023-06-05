using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Utilities;

using StardewValley.Menus;

using StopRugRemoval.Framework.Menus;

namespace StopRugRemoval.HarmonyPatches;

[HarmonyPatch(typeof(ShopMenu))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class DresserMenuDoll
{
    private static readonly PerScreen<MiniFarmerMenu?> _miniMenu = new();

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopMenu.setUpStoreForContext))]
    private static void PostfixSetup(ShopMenu __instance)
    {
        if (__instance.storeContext == "Dresser")
        {
            _miniMenu.Value = new(__instance);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopMenu.draw))]
    private static void PostfixDraw(ShopMenu __instance, SpriteBatch b)
    {
        if (_miniMenu.Value?.shopMenu is not null && ReferenceEquals(__instance, _miniMenu.Value?.shopMenu))
        {
            _miniMenu.Value.draw(b);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopMenu.gameWindowSizeChanged))]
    private static void PostfixResize(ShopMenu __instance, Rectangle oldBounds, Rectangle newBounds)
    {
        if (_miniMenu.Value?.shopMenu is not null && ReferenceEquals(__instance, _miniMenu.Value?.shopMenu))
        {
            _miniMenu.Value.gameWindowSizeChanged(oldBounds, newBounds);
        }
    }
}
