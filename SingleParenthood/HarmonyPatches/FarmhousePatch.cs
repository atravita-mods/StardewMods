using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Locations;

namespace SingleParenthood.HarmonyPatches;

/// <summary>
/// Patches farmhouse to prevent the player from removing the bed if they're expecting a kid.
/// </summary>
[HarmonyPatch(typeof(FarmHouse))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class FarmhousePatch
{
    [HarmonyPatch(nameof(FarmHouse.CanModifyCrib))]
    private static bool Prefix(FarmHouse __instance, ref bool __result)
    {
        if (__instance.owner.modData?.GetInt(ModEntry.CountUp) is > 0 )
        {
            __result = false;
            return false;
        }
        return true;
    }
}
