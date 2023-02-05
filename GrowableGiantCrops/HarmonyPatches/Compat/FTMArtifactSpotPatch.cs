using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using AtraBase.Toolkit.Reflection;
using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.HarmonyHelper;


using GrowableGiantCrops.Framework;
using GrowableGiantCrops.HarmonyPatches.ItemPatches;
using HarmonyLib;

using StardewValley.Tools;

namespace GrowableGiantCrops.HarmonyPatches.Compat;
internal static class FTMArtifactSpotPatch
{
    /// <summary>
    /// Applies the patches for this class.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    internal static void ApplyPatch(Harmony harmony)
    {
        Type? buriedItem = AccessTools.TypeByName("FarmTypeManager.ModEntry+BuriedItems");
        if (buriedItem is null)
        {
            ModEntry.ModMonitor.Log($"Farm Type Manager's buried items may not behave correctly if dug up with the shovel.", LogLevel.Error);
            return;
        }

        try
        {
            harmony.Patch(
                original: buriedItem.GetCachedMethod("performToolAction", ReflectionCache.FlagTypes.InstanceFlags),
                prefix: new HarmonyMethod(typeof(FTMArtifactSpotPatch).StaticMethodNamed(nameof(Prefix))));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed to patch FTM to support artifact spots.\n\n{ex}", LogLevel.Error);
        }
    }

    private static bool Prefix(SObject __instance, Tool t, GameLocation location, ref bool __result)
    {
        if (t is not ShovelTool)
        {
            return true;
        }

        try
        {
            __result = true;
            __instance.GetType()
                      .GetCachedMethod("releaseContents", ReflectionCache.FlagTypes.InstanceFlags)
                      .Invoke(__instance, new[] { location });
            if (!location.terrainFeatures.ContainsKey(__instance.TileLocation))
            {
                location.makeHoeDirt(__instance.TileLocation);
            }
            location.playSound("hoeHit");
            return false;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed while trying to use shovel on FTM artifact spot:\n\n{ex}", LogLevel.Error);
        }

        return true;
    }
}
