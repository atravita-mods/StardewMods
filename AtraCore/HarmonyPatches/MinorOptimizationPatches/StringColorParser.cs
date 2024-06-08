using AtraShared.Utils;

using HarmonyLib;

using Microsoft.Xna.Framework;

namespace AtraCore.HarmonyPatches.MinorOptimizationPatches;

[HarmonyPatch(typeof(Utility))]
internal static class ColorParserPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Utility.StringToColor))]
    private static bool OverrideStringToColor(string rawColor, ref Color? __result)
    {
        if (ColorHandler.TryParseColor(rawColor, out var color))
        {
            __result = color;
            return false;
        }

        return true;
    }
}