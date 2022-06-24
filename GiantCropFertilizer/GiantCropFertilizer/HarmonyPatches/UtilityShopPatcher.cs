using HarmonyLib;

namespace GiantCropFertilizer.HarmonyPatches;

/// <summary>
/// Patch to put our fertilzier into Qi's shop.
/// </summary>
[HarmonyPatch(typeof(Utility))]
internal static class UtilityShopPatcher
{
    [HarmonyPatch(nameof(Utility.GetQiChallengeRewardStock))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention.")]
    private static void Postfix(ref Dictionary<ISalable, int[]> __result)
    {
        if (ModEntry.GiantCropFertilizerID != -1)
        {
            SObject obj = new(ModEntry.GiantCropFertilizerID, 1);
            __result.Add(obj, new[] { 0, int.MaxValue, 858, 5 });
        }
    }
}