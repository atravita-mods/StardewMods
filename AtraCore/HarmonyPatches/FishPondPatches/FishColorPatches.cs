using AtraShared.Utils;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Buildings;
using StardewValley.GameData.Objects;

namespace AtraCore.HarmonyPatches.FishPondPatches;

[HarmonyPatch(typeof(FishPond))]
internal static class FishColorPatches
{
    [HarmonyPatch("doFishSpecificWaterColoring")]
    private static void Postfix(FishPond __instance)
    {
        try
        {
            if (!Game1.objectData.TryGetValue(__instance.fishType.Value, out ObjectData? data))
            {
                return;
            }

            if (data.CustomFields?.TryGetValue("atravita.FishPondColor", out string? scolor) is not true)
            {
                if (ModEntry.Config.AutoColorFishPonds)
                {
                    scolor = "tailoring";
                }
                else
                {
                    return;
                }
            }

            if (data.CustomFields?.TryGetValue("atravita.FishPondColor.MinPopulation", out string? pop) is true)
            {
                if (!int.TryParse(pop, out int val))
                {
                    ModEntry.ModMonitor.Log($"Fish {__instance.fishType.Value} has invalid population gate for colorful fish ponds {pop}", LogLevel.Warn);
                    return;
                }
                else if (__instance.maxOccupants.Value < val)
                {
                    return;
                }
            }

            Color? color = null;
            if (scolor.Equals("tailoring", StringComparison.OrdinalIgnoreCase))
            {
                color = ItemContextTagManager.GetColorFromTags(ItemRegistry.Create(ItemRegistry.type_object + __instance.fishType.Value));
            }

            if (color is null)
            {
                if (!ColorHandler.TryParseColor(scolor, out Color potentialColor))
                {
                    ModEntry.ModMonitor.Log($"{scolor} could not be parsed as a valid color for {__instance.fishType.Value}'s custom fish coloring.");
                    return;
                }
                color = potentialColor;
            }

            __instance.overrideWaterColor.Value = color.Value;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("coloring ponds", ex);
        }
    }
}
