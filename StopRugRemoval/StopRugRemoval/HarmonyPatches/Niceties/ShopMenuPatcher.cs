using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using StardewValley.Menus;
using StardewValley.Tools;

namespace StopRugRemoval.HarmonyPatches.Niceties;

[HarmonyPatch(typeof(ShopMenu))]
internal static class ShopMenuPatcher
{
    [HarmonyPatch(nameof(ShopMenu.setUpStoreForContext))]
    private static void Postfix(ShopMenu __instance)
    {
        if (ModEntry.Config.Enabled
            && __instance.storeContext == "Dresser")
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
}
