namespace GingerIslandMainlandAdjustments.Niceties;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Locations;

/// <summary>
/// Holds patches against GameLocation to prevent trampling of objects on IslandWest.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal class GameLocationPatches
{
    /// <summary>
    /// Prefix to prevent trampling.
    /// </summary>
    /// <param name="__instance">Gamelocation.</param>
    /// <returns>True to continue to original function, false to skip original function.</returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameLocation.characterTrampleTile), new Type[] { typeof(Vector2) })]
    private static bool PrefixCharacterTrample(GameLocation __instance)
    {
        try
        {
            return __instance is not IslandWest;
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.LogError($"preventing trampling at {__instance.NameOrUniqueName}", ex);
        }
        return true;
    }

    /// <summary>
    /// Prefix to prevent characters from destroying things.
    /// </summary>
    /// <param name="__instance">GameLocation.</param>
    /// <returns>True to continue to original function, false to skip original function.</returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameLocation.characterDestroyObjectWithinRectangle), new Type[] { typeof(Rectangle), typeof(bool) })]
    private static bool PrefixCharacterDestroy(GameLocation __instance)
    {
        try
        {
            return __instance is not IslandWest;
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.LogError($"preventing trampling at {__instance.NameOrUniqueName}", ex);
        }
        return true;
    }
}