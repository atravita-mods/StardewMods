using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Objects;

namespace StopRugRemoval.HarmonyPatches;

[HarmonyPatch(typeof(SObject))]
internal class SObjectPatches
{
    [HarmonyPatch("canPlaceWildTreeSeed")]
    private static bool Prefix(GameLocation location, Vector2 tile, ref bool __result)
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
}