using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AtraShared.Utils;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Buildings;

namespace AtraCore.HarmonyPatches.FishPondPatches;

[HarmonyPatch(typeof(FishPond))]
internal static class FishColorPatches
{
    [HarmonyPatch("doFishSpecificWaterColoring")]
    private static void Postfix(FishPond __instance)
    {
        try
        {
            if (!Game1.objectData.TryGetValue(__instance.fishType.Value, out var data)
                || data.CustomFields?.TryGetValue("atravita.FishPondColor", out var scolor) is not true)
            {
                return;
            }

            if (data.CustomFields.TryGetValue("atravita.FishPondColor.MinPopulation", out var pop))
            {
                if (!int.TryParse(pop, out var val))
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
                if (!ColorHandler.TryParseColor(scolor, out var potentialColor))
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
