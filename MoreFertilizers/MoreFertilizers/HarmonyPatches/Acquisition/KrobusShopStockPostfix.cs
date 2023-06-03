using AtraShared.Caching;
using AtraShared.ConstantsAndEnums;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Locations;
using StardewValley.Menus;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Postfix to add things to Krobus's shop.
/// </summary>
[HarmonyPatch(typeof(Sewer))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class KrobusShopStockPostfix
{
    private static readonly TickCache<bool> HasGottenPrismaticFertilizer = new(static () => FarmerHelpers.HasAnyFarmerRecievedFlag($"museumCollectedRewardO_{ModEntry.PrismaticFertilizerID}_1"));

    [HarmonyPatch(nameof(Sewer.getShadowShopStock))]
    private static void Postfix(ref Dictionary<ISalable, int[]> __result)
    {
        try
        {
            if (ModEntry.PaddyCropFertilizerID != -1)
            {
                __result.TryAdd(new SObject(ModEntry.PaddyCropFertilizerID, 1), new[] { 40, ShopMenu.infiniteStock });
            }
            if (ModEntry.WisdomFertilizerID != -1 && Game1.currentSeason is "spring" or "fall")
            {
                __result.TryAdd(new SObject(ModEntry.WisdomFertilizerID, 1), new[] { 100, ShopMenu.infiniteStock });
            }
            if (ModEntry.MiraculousBeveragesID != -1 && Game1.year > 2 && Utility.getCookedRecipesPercent() > 0.5f)
            {
                __result.TryAdd(new SObject(ModEntry.MiraculousBeveragesID, 1), new[] { 250, ShopMenu.infiniteStock });
            }
            if (ModEntry.RadioactiveFertilizerID != -1 && Game1.year > 2 && Game1.player.hasMagicInk)
            {
                __result.TryAdd(new SObject(ModEntry.RadioactiveFertilizerID, 1), new[] { 250, ShopMenu.infiniteStock });
            }
            if (ModEntry.PrismaticFertilizerID != -1 && HasGottenPrismaticFertilizer.GetValue())
            {
                __result.TryAdd(new SObject(ModEntry.PrismaticFertilizerID, 1), new[] { 100, ShopMenu.infiniteStock });
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding to krobus' shop", ex);
        }
    }
}