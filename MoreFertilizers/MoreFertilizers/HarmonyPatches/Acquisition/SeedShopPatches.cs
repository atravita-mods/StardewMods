using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Locations;
using StardewValley.Menus;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Patches against Locations.SeedShop.
/// </summary>
[HarmonyPatch(typeof(SeedShop))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class SeedShopPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(SeedShop.shopStock))]
    private static void PostfixSeedShop(Dictionary<ISalable, int[]> __result)
    {
        try
        {
            if (ModEntry.LuckyFertilizerID != -1
                && !(Game1.year == 1 && Game1.currentSeason.Equals("spring", StringComparison.OrdinalIgnoreCase))
                && Game1.player.team.AverageDailyLuck() > 0.07)
            {
                __result.Add(new SObject(Vector2.Zero, ModEntry.LuckyFertilizerID, 1), new[] { Game1.year == 1 ? 100 : 150, ShopMenu.infiniteStock });
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding to seedShop", ex);
        }
    }
}
