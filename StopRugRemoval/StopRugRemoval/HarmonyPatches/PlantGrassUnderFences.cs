using System.Reflection;
using AtraBase.Toolkit.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;

namespace StopRugRemoval.HarmonyPatches;

[HarmonyPatch]
internal class PlantGrassUnderFences
{

    public static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (Type t in typeof(SObject).GetAssignableTypes(publiconly: true, includeAbstract: false))
        {
            if (AccessTools.Method(t, nameof(SObject.performObjectDropInAction), new Type[] { typeof(Item), typeof(bool), typeof(Farmer) }) is MethodBase method
                && method.DeclaringType == t)
            {
                yield return method;
            }
        }
    }

    /// <summary>
    /// Postfixes Perform ObjectDropInAction to allow for grass starters to placed under things.
    /// </summary>
    /// <param name="__instance">Object being placed under.</param>
    /// <param name="__0">Item to drop in.</param>
    /// <param name="__1">"Probe": just checking? (Zero clue).</param>
    /// <param name="__2">The farmer doing the placing.</param>
    /// <param name="__result">The result to substitute in.</param>
    [HarmonyPostfix]
    [SuppressMessage("StyleCop", "SA1313", Justification = "Style prefered by Harmony")]
    public static void PostfixPerformObjectDropInAction(SObject __instance, Item __0, bool __1, Farmer __2, ref bool __result)
    {
        if (__result // Placed something already
           || __1 // just checking!
           || __2.currentLocation is null
           || !ModEntry.Config.Enabled)
        {
            return;
        }
        try
        {
            // Grass starter = 297
            if (Utility.IsNormalObjectAtParentSheetIndex(__0, 297))
            {
                GameLocation location = __2.currentLocation;
                Vector2 placementTile = __instance.TileLocation;

                if (!location.terrainFeatures.ContainsKey(placementTile) && !location.isWaterTile((int)placementTile.X, (int)placementTile.Y))
                {
                    location.terrainFeatures.Add(placementTile, new Grass(1, 4));
                    location.playSound("dirtyHit");
                    __result = true;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Rain into errors attempting to place grass under object at {__instance.TileLocation}.\n\n{ex}", LogLevel.Error);
        }
    }
}