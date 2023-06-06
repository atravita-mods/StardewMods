using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using StardewValley.Menus;

namespace DresserMiniMenu.HarmonyPatches.Niceties;

/// <summary>
/// Adds patches to shop menu to make dressers take takes and weapons.
/// </summary>
[HarmonyPatch(typeof(ShopMenu))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class ShopMenuPatcher
{
    [HarmonyPatch(nameof(ShopMenu.setUpStoreForContext))]
    private static void Postfix(ShopMenu __instance)
    {
        if (__instance.storeContext == "Dresser")
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
