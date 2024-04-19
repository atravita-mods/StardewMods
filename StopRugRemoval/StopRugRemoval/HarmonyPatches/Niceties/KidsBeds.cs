using HarmonyLib;

using StardewValley.Locations;
using StardewValley.Objects;

namespace StopRugRemoval.HarmonyPatches.Niceties;

[HarmonyPatch(typeof(FarmHouse))]
internal static class KidsBeds
{
    [HarmonyPatch(nameof(FarmHouse.GetChildBed))]
    private static void Postfix(FarmHouse __instance, ref BedFurniture __result)
    {
        __result ??= __instance.GetBed(BedFurniture.BedType.Single);
    }
}
