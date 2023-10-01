using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using StardewValley.Menus;

namespace DresserMiniMenu.HarmonyPatches;

/// <summary>
/// Holds patches against Farmer.
/// </summary>
[HarmonyPatch(typeof(Farmer))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class FarmerPatcher
{
    [HarmonyPatch(nameof(Farmer.couldInventoryAcceptThisItem), new[] { typeof(Item) })]
    private static bool Prefix(Farmer __instance, Item item, ref bool __result)
    {
        if (Game1.activeClickableMenu is not ShopMenu shop || !DresserMenuDoll.IsActive(shop, out Framework.MiniFarmerMenu? mini)
            || !ReferenceEquals(mini.FarmerRef, __instance))
        {
            return true;
        }

        if (mini.CanAcceptThisItem(item))
        {
            __result = true;
            return false;
        }

        return true;
    }
}