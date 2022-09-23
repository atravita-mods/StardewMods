using HarmonyLib;
using StardewValley.Locations;

namespace MoreFertilizers.HarmonyPatches.Acquisition;

/// <summary>
/// Postfix to add things to Krobus's shop.
/// </summary>
[HarmonyPatch(typeof(Sewer))]
internal static class KrobusShopStockPostfix
{
    [HarmonyPatch(nameof(Sewer.getShadowShopStock))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
    private static void Postfix(ref Dictionary<ISalable, int[]> __result)
    {
        if (ModEntry.PaddyCropFertilizerID != -1)
        {
            __result.TryAdd(new SObject(ModEntry.PaddyCropFertilizerID, 1), new[] { 40, int.MaxValue });
        }
        if (ModEntry.WisdomFertilizerID != -1)
        {
            __result.TryAdd(new SObject(ModEntry.WisdomFertilizerID, 1), new[] { 80, int.MaxValue });
        }
        if (ModEntry.MiraculousBeveragesID != -1)
        {
            __result.TryAdd(new SObject(ModEntry.MiraculousBeveragesID, 1), new[] { 150, int.MaxValue });
        }
    }
}