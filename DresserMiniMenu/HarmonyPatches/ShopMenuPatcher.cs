﻿using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using StardewValley.Menus;
using StardewValley.Tools;

namespace DresserMiniMenu.HarmonyPatches;

/// <summary>
/// Adds patches to shop menu to make dressers take takes and weapons.
/// </summary>
[HarmonyPatch(typeof(ShopMenu))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class ShopMenuPatcher
{
    /// <summary>
    /// The store context for a dresser.
    /// </summary>
    internal const string DRESSER = "Dresser";

    [HarmonyPatch(nameof(ShopMenu.setUpStoreForContext))]
    private static void Postfix(ShopMenu __instance)
    {
        if (__instance.storeContext == DRESSER)
        {
            if (ModEntry.Config.DressersAllowBobbers)
            {
                __instance.categoriesToSellHere.Add(SObject.tackleCategory);
            }
            if (ModEntry.Config.DressersAllowWeapons)
            {
                __instance.categoriesToSellHere.Add(SObject.weaponCategory);
            }
        }
    }

    [HarmonyPatch(nameof(ShopMenu.highlightItemToSell))]
    private static bool Prefix(ShopMenu __instance, Item i, ref bool __result)
    {
        if (i is Pan && __instance.storeContext == DRESSER)
        {
            __result = true;
            return false;
        }
        return true;
    }
}
