using System.Reflection;

using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;
using StardewValley.Objects;

namespace StopRugRemoval.HarmonyPatches.OutdoorRugsMostly;

/// <summary>
/// I think this prevents the cursor from turning green when trying to place a tree on a rug.
/// </summary>
[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class CanPlantTreesHerePatches
{
    /// <summary>
    /// Defines the methods for which to patch.
    /// </summary>
    /// <returns>Methods to patch.</returns>
    [UsedImplicitly]
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (Type type in typeof(GameLocation).GetAssignableTypes(publiconly: true, includeAbstract: false))
        {
            if (type.GetCachedMethod(nameof(GameLocation.CanPlantTreesHere), ReflectionCache.FlagTypes.UnflattenedInstanceFlags, new Type[] { typeof(string), typeof(int), typeof(int), typeof(string).MakeByRefType() }) is MethodBase method
                && method.DeclaringType == type)
            {
                yield return method;
            }
        }
    }

    /// <summary>
    /// Prefix to prevent planting trees on rugs.
    /// </summary>
    /// <param name="__instance">Game location.</param>
    /// <param name="tileX">Tile X.</param>
    /// <param name="tileY">Tile Y.</param>
    /// <param name="__result">Result to replace the original with.</param>
    /// <returns>True to continue to original, false to skip.</returns>
    internal static bool Prefix(GameLocation __instance, int tileX, int tileY, ref bool __result)
    {
        try
        {
            int xpos = (tileX * 64) + 32;
            int ypos = (tileY * 64) + 32;
            foreach (Furniture f in __instance.furniture)
            {
                if (f.GetBoundingBox().Contains(xpos, ypos))
                {
                    __result = false;
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("preventing planting trees on rugs", ex);
        }
        return true;
    }
}