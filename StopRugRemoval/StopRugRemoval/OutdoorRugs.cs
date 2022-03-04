using AtraBase.Collections;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Locations;
using StardewValley.Objects;

namespace StopRugRemoval;

/// <summary>
/// Handles applying and removing NoSpawn from each rug's tile.
/// </summary>
internal static class OutdoorRugs
{
    private static readonly DefaultDict<string, List<Vector2>> noSpawns = new(() => new List<Vector2>());

    internal static void ApplyNoSpawns()
    {
        ModEntry.ModMonitor.DebugLog("Applying NoSpawns", LogLevel.Alert);
        foreach (GameLocation gameLocation in Game1.locations)
        {
            foreach (Furniture furniture in gameLocation.furniture)
            {
                if (furniture.furniture_type.Value != Furniture.rug)
                {
                    continue;
                }
                Rectangle bounds = furniture.boundingBox.Value;
                for (int x = 0; x < bounds.Width / 64; x++)
                {
                    for (int y = 0; y < bounds.Height / 64; y++)
                    {
                        // check for large terrain+terrain, refuse placement.
                        // check for is placeable everywhere, and if the thing that's blocking placement is an
                        // another furniture item, I'm still okay to place.
                    }
                }
                noSpawns[gameLocation.NameOrUniqueName].Add(furniture.TileLocation);
                ModEntry.ModMonitor.DebugLog($"{gameLocation.NameOrUniqueName}, {furniture.TileLocation}");
            }
        }
    }

    internal static void RemoveNoSpawns()
    {

    }

}

#if DEBUG // not yet finished implementing....
/// <summary>
/// Patches on GameLocation to allow me to place rugs anywhere.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
internal class GameLocationPatches
{
    [SuppressMessage("StyleCop", "SA1313", Justification = "Style prefered by Harmony")]
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
            ModEntry.ModMonitor.Log($"Failed in attempting to place rug outside in PostfixCanPlaceFurnitureHere.\n{ex}", LogLevel.Error);
        }
    }
}

#endif