using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Objects;

namespace StopRugRemoval.HarmonyPatches;

[HarmonyPatch(typeof(SObject))]
internal class SObjectPatches
{
    [HarmonyPrefix]
    [HarmonyPatch("canPlaceWildTreeSeed")]
    [SuppressMessage("StyleCop", "SA1313", Justification = "Style prefered by Harmony")]
    public static bool PrefixWildTrees(GameLocation location, Vector2 tile, ref bool __result)
    {
        try
        {
            if (!ModEntry.Config.PreventPlantingOnRugs)
            {
                return true;
            }
            (int posX, int posY) = ((tile * 64f) + new Vector2(32f, 32f)).ToPoint();
            foreach (Furniture f in location.furniture)
            {
                if (f.furniture_type.Value == Furniture.rug && f.getBoundingBox(f.TileLocation).Contains(posX, posY))
                {
                    __result = false;
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Errored while trying to prevent tree growth\n\n{ex}.", LogLevel.Error);
        }
        return true;
    }

    /// <summary>
    /// Prefix on placement to prevent planting of fruit trees and tea saplings on rugs, hopefully.
    /// </summary>
    /// <param name="__instance">SObject instance to check.</param>
    /// <param name="location">Gamelocation being placed in.</param>
    /// <param name="x">X placement location in pixel coordinates.</param>
    /// <param name="y">Y placement location in pixel coordinates.</param>
    /// <param name="__result">Result of the function.</param>
    /// <returns>True to continue to vanilla function, false otherwise.</returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(SObject.placementAction))]
    [SuppressMessage("StyleCop", "SA1313", Justification = "Style prefered by Harmony")]
    public static bool PrefixPlacementAction(SObject __instance, GameLocation location, int x, int y, ref bool __result)
    {
        try
        {
            if (ModEntry.Config.PreventPlantingOnRugs && __instance.isSapling())
            {
                foreach (Furniture f in location.furniture)
                {
                    if (f.getBoundingBox(f.TileLocation).Contains(x, y))
                    {
                        Game1.showRedMessage(I18n.RugPlantingMessage());
                        __result = false;
                        return false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod failed while trying to prevent tree planting\n\n{ex}", LogLevel.Error);
        }
        return true;
    }


}