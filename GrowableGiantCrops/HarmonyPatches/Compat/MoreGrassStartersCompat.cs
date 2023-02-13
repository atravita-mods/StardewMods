using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AtraBase.Toolkit.Reflection;
using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;

using GrowableGiantCrops.HarmonyPatches.GrassPatches;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.HarmonyPatches.Compat;
internal static class MoreGrassStartersCompat
{
    /// <summary>
    /// Applies the patches for this class.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    internal static void ApplyPatch(Harmony harmony)
    {
        Type? buriedItem = AccessTools.TypeByName("MoreGrassStarters.GrassStarterItem");
        if (buriedItem is null)
        {
            ModEntry.ModMonitor.Log($"MoreGrassStarter's GrassStarter item could not be found?.", LogLevel.Error);
            return;
        }

        try
        {
            harmony.Patch(
                original: buriedItem.GetCachedMethod("placementAction", ReflectionCache.FlagTypes.InstanceFlags),
                prefix: new HarmonyMethod(typeof(MoreGrassStartersCompat).StaticMethodNamed(nameof(Postfix))));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed to patch More Grass Starters.\n\n{ex}", LogLevel.Error);
        }
    }

    private static void Postfix(SObject __instance, GameLocation location, int x, int y, bool __result)
    {
        if (!__result || __instance?.modData?.GetBool(SObjectPatches.ModDataKey) != true)
        {
            return;
        }

        try
        {
            Vector2 tile = new Vector2(x / Game1.tileSize, y / Game1.tileSize);
            if (location.terrainFeatures?.TryGetValue(tile, out TerrainFeature? terrain) == true
                && terrain is Grass grass)
            {
                grass.numberOfWeeds.Value = 1;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed while trying to override health of MGS grass:\n\n{ex}", LogLevel.Error);
        }
    }
}
