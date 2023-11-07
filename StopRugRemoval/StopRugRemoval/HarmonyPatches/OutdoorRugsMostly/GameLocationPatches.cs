using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Locations;
using StardewValley.Objects;

namespace StopRugRemoval.HarmonyPatches.OutdoorRugsMostly;

/// <summary>
/// Patches on GameLocation to allow me to place rugs anywhere.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal class GameLocationPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(GameLocation.CanPlaceThisFurnitureHere))]
    private static void PostfixCanPlaceFurnitureHere(GameLocation __instance, Furniture __0, ref bool __result)
    {
        try
        {
            if (__result // can already be placed
                || __0.placementRestriction != 0 // someone requested a custom placement restriction, respect that.
                || !ModEntry.Config.Enabled || !ModEntry.Config.CanPlaceRugsOutside // mod disabled
                || __instance is MineShaft || __instance is VolcanoDungeon // do not want to affect mines
                || !__0.furniture_type.Value.Equals(Furniture.rug) // only want to affect rugs
                )
            {
                return;
            }
            __result = true;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("placing rug outside", ex);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameLocation.makeHoeDirt))]
    private static bool PrefixMakeHoeDirt(GameLocation __instance, Vector2 tileLocation, bool ignoreChecks = false)
    {
        try
        {
            if (ignoreChecks || !ModEntry.Config.PreventPlantingOnRugs)
            {
                return true;
            }

            int posX = ((int)tileLocation.X * 64) + 32;
            int posY = ((int)tileLocation.Y * 64) + 32;
            foreach (Furniture f in __instance.furniture)
            {
                if (f.furniture_type.Value == Furniture.rug && f.GetBoundingBox().Contains(posX, posY))
                {
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("preventing hoeing on rugs", ex);
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(GameLocation.doesTileHavePropertyNoNull))]
    private static bool PrefixDoesTileHavePropertyNoNull(GameLocation __instance, int xTile, int yTile, string propertyName, string layerName, ref string __result)
    {
        try
        {
            if (propertyName.Equals("NoSpawn", StringComparison.OrdinalIgnoreCase) && layerName.Equals("Back", StringComparison.OrdinalIgnoreCase))
            {
                foreach (Furniture f in __instance.furniture)
                {
                    if (f.furniture_type.Value == Furniture.rug && f.GetBoundingBox().Contains((xTile * 64) + 32, (yTile * 64) + 32))
                    {
                        __result = "All";
                        return false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("preventing grass growth", ex);
        }
        return true;
    }
}
