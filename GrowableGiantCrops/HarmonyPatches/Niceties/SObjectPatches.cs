using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GrowableGiantCrops.Framework;

using HarmonyLib;

namespace GrowableGiantCrops.HarmonyPatches.Niceties;

// TODO : Move this to Tool.DoFunction.

[HarmonyPatch(typeof(SObject))]
internal static class SObjectPatches
{
    [HarmonyPatch(nameof(SObject.performToolAction))]
    private static bool Prefix(SObject __instance, Tool t, GameLocation location, ref bool __result)
    {
        if (t is not ShovelTool)
        {
            return true;
        }

        return true;
    }
}
