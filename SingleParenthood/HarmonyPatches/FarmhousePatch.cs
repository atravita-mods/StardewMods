using AtraShared.Utils.Extensions;
using HarmonyLib;
using StardewValley.Locations;

namespace SingleParenthood.HarmonyPatches;

/// <summary>
/// Patches farmhouse to prevent the player from removing the bed if they're expecting a kid.
/// </summary>
[HarmonyPatch(typeof(FarmHouse))]
internal static class FarmhousePatch
{
    [HarmonyPatch(nameof(FarmHouse.CanModifyCrib))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
    private static bool Prefix(FarmHouse __instance, ref bool __result)
    {
        if (__instance.owner.modData?.GetInt(ModEntry.countdown) is >= 0 )
        {
            __result = false;
            return true;
        }
        return false;
    }
}
