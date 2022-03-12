using System.Reflection;
using AtraBase.Toolkit.Reflection;
using HarmonyLib;
using StardewValley.Objects;

namespace StopRugRemoval.HarmonyPatches;

[HarmonyPatch]
internal static class CanPlantTreesHerePatches
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (Type type in typeof(GameLocation).GetAssignableTypes(publiconly: true, includeAbstract: false))
        {
            if (AccessTools.Method(type, nameof(GameLocation.CanPlantTreesHere), new Type[] { typeof(int), typeof(int), typeof(int) }) is MethodBase method
                && method.DeclaringType == type)
            {
                yield return method;
            }
        }
    }

    [SuppressMessage("StyleCop", "SA1313", Justification = "Style prefered by Harmony")]
    public static bool Prefix(GameLocation __instance, int tile_x, int tile_y, ref bool __result)
    {
        try
        {
            int xpos = (tile_x * 64) + 32;
            int ypos = (tile_y * 64) + 32;
            foreach (Furniture f in __instance.furniture)
            {
                if (f.getBoundingBox(f.TileLocation).Contains(xpos, ypos))
                {
                    __result = false;
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Encountered error in prefix on GameLocation.CanPlantTrees Here\n\n{ex}", LogLevel.Error);
        }
        return true;
    }
}