using HarmonyLib;

using StardewValley.Menus;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Adds patches to shop menu to make dressers take takes and weapons.
/// </summary>
[HarmonyPatch(typeof(ShopMenu))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
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
