using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Menus;

namespace GrowableGiantCrops.HarmonyPatches.Niceties;

/// <summary>
/// Patches on the dwarf's shop stock.
/// </summary>
[HarmonyPatch(typeof(Utility))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class DwarfShopPatches
{
    [HarmonyPriority(Priority.VeryLow)]
    [HarmonyPatch(nameof(Utility.getDwarfShopStock))]
    private static void Postfix(Dictionary<ISalable, int[]> __result)
    {
        try
        {
            if (Game1.player.hasMagicInk)
            {
                SObject boulder = new(Vector2.Zero, 78) { Fragility = SObject.fragility_Removable };
                __result.TryAdd(boulder, new[] { 1000, ShopMenu.infiniteStock });
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding a decorative boulder to the dwarf's shop stock", ex);
        }
    }
}
