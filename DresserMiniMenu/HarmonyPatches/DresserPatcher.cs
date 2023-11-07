using DresserMiniMenu.Framework;

using HarmonyLib;

using StardewValley.Menus;
using StardewValley.Objects;

namespace DresserMiniMenu.HarmonyPatches;

/// <summary>
/// Patches on the dresser itself.
/// </summary>
[HarmonyPatch(typeof(StorageFurniture))]
internal static class DresserPatcher
{
    [HarmonyPatch(nameof(StorageFurniture.onDresserItemDeposited))]
    private static void Postfix()
    {
        if (Game1.activeClickableMenu is ShopMenu shopMenu && DresserMenuDoll.IsActive(shopMenu, out MiniFarmerMenu? mini))
        {
            mini.ApplyFilter();
        }
    }
}
