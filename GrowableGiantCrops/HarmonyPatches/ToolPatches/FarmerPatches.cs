using GrowableGiantCrops.Framework;

using HarmonyLib;

namespace GrowableGiantCrops.HarmonyPatches.ToolPatches;

/// <summary>
/// Patches on farmer.
/// </summary>
[HarmonyPatch(typeof(Farmer))]
internal static class FarmerPatches
{
    /// <summary>
    /// Disable the tool swipe for the shovel.
    /// </summary>
    /// <param name="who">Farmer.</param>
    /// <returns>false if the current tool is the shoveltool to prevent the swipes from showing up.</returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Farmer.showToolSwipeEffect))]
    private static bool PrefixToolSwipe(Farmer who) => who.CurrentTool is not ShovelTool;
}
