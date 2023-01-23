using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GrowableGiantCrops.Framework;

using HarmonyLib;

namespace GrowableGiantCrops.HarmonyPatches.ToolPatches;

[HarmonyPatch(typeof(Farmer))]
internal static class FarmerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Farmer.showToolSwipeEffect))]
    private static bool PrefixToolSwipe(Farmer who) => who.CurrentTool is not ShovelTool;
}
