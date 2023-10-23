using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using StardewValley.Menus;

namespace DresserMiniMenu.HarmonyPatches;

/// <summary>
/// Holds patches against IClickableMenu.
/// </summary>
[HarmonyPatch(typeof(IClickableMenu))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class IClickableMenuPatcher
{
    [HarmonyPatch(nameof(IClickableMenu.populateClickableComponentList))]
    private static void Postfix(IClickableMenu __instance)
    {
        if (__instance is ShopMenu menu && DresserMenuDoll.IsActive(menu, out Framework.MiniFarmerMenu? mini))
        {
            mini.AddClickables(menu);
        }
    }
}
