using HarmonyLib;

namespace PrismaticSlime.HarmonyPatches.SlimeToastPatches;

[HarmonyPatch(typeof(Farmer))]
internal static class FarmerPatches
{
    [HarmonyPatch(nameof(Farmer.doneEating))]
    private static void Prefix(Farmer __instance)
    {
        if (!Utility.IsNormalObjectAtParentSheetIndex(__instance.itemToEat, ModEntry.PrismaticJellyToast))
        {
            return;
        }

        try
        {
            
        }
        catch (Exception ex)
        {

        }
    }
}
