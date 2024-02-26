using System.Reflection;

using AtraBase.Toolkit.Reflection;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace StopRugRemoval.HarmonyPatches;

/// <summary>
/// Class to hold patches to place grass.
/// </summary>
[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class PlantGrassUnder
{
    private static Func<bool>? isSmartBuildingInBuildMode = null;

    private static IGrowableGiantCrops? growableGiantCropsAPI;
    private static IMoreGrassStartersAPI? moreGrassStartersAPI;

    /// <summary>
    /// Gets the methods to patch.
    /// </summary>
    /// <returns>An IEnumerable of methods to patch.</returns>
    public static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (Type t in typeof(SObject).GetAssignableTypes(publiconly: true, includeAbstract: false))
        {
            if (t != typeof(CrabPot) // does not make sense to place under crab pots.
                && t.DeclaredInstanceMethodNamedOrNull(nameof(SObject.performObjectDropInAction), [typeof(Item), typeof(bool), typeof(Farmer), typeof(bool)]) is MethodBase method
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
    public static void PostfixPerformObjectDropInAction(SObject __instance, Item __0, bool __1, Farmer __2, ref bool __result)
    {
        if (__result // Placed something already
           || __1 // just checking!
           || __2.currentLocation is null
           || !ModEntry.Config.Enabled
           || !ModEntry.Config.PlaceGrassUnder)
        {
            return;
        }
        try
        {
            // Grass starter = 297
            if (__0 is SObject starter && !(isSmartBuildingInBuildMode?.Invoke() == true))
            {
                Grass? grass = null;
                if (starter.QualifiedItemId == "(O)297")
                {
                    grass ??= growableGiantCropsAPI?.GetMatchingGrass(starter) ?? new Grass(Grass.springGrass, 4);
                }
                else if (moreGrassStartersAPI?.GetMatchingGrass(starter) is Grass moreGrassStartersGrass)
                {
                    grass = moreGrassStartersGrass;
                }
                else
                {
                    return;
                }

                GameLocation location = __2.currentLocation;
                Vector2 placementTile = __instance.TileLocation;

                if (!location.terrainFeatures.ContainsKey(placementTile) && !location.isWaterTile((int)placementTile.X, (int)placementTile.Y))
                {
                    location.terrainFeatures.Add(placementTile, grass);
                    location.playSound("dirtyHit");
                    __result = true;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"placing grass under object at {__instance.TileLocation}", ex);
        }
    }

    /// <summary>
    /// Grabs a reference to Smart Building's CurrentlyInBuildMode.
    /// </summary>
    /// <param name="translation">Translation helper.</param>
    /// <param name="registry">ModRegistry.</param>
    internal static void GetSmartBuildingBuildMode(ITranslationHelper translation, IModRegistry registry)
    {
        try
        {
            if (registry.Get("DecidedlyHuman.SmartBuilding") is not IModInfo info || info.Manifest.Version.IsOlderThan("1.3.2"))
            {
                ModEntry.ModMonitor.Log("SmartBuilding not installed, no need to adjust for that", LogLevel.Trace);
            }
            else if (Type.GetType("SmartBuilding.HarmonyPatches.Patches, SmartBuilding") is Type type
                && AccessTools.DeclaredPropertyGetter(type, "CurrentlyInBuildMode") is MethodInfo method)
            {
                ModEntry.ModMonitor.Log("SmartBuilding found! " + method.FullDescription(), LogLevel.Trace);
                isSmartBuildingInBuildMode = method.CreateDelegate<Func<bool>>();
            }
            else
            {
                ModEntry.ModMonitor.Log("SmartBuilding is installed BUT compat unsuccessful. You may see issues, please bring this log to atravita!", LogLevel.Info);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("grabbing SmartBuilding's CurrentlyInBuildMode", ex);
        }

        IntegrationHelper helper = new(ModEntry.ModMonitor, translation, registry, LogLevel.Trace);

        _ = helper.TryGetAPI("atravita.GrowableGiantCrops", null, out growableGiantCropsAPI)
            || helper.TryGetAPI("spacechase0.MoreGrassStarters", "1.2.2", out moreGrassStartersAPI);
    }
}