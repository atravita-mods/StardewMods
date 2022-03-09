using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Objects;

namespace StopRugRemoval.HarmonyPatches;

/// <summary>
/// Class to hold patches for the Furniture class, to allow me to place rugs under other furniture
/// And to prevent me from removing rugs when I'm not supposed to....
/// </summary>
[HarmonyPatch(typeof(Furniture))]
internal class FurniturePatches
{
    [SuppressMessage("StyleCop", "SA1313", Justification = "Style prefered by Harmony")]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Furniture.canBeRemoved))]
    private static void PostfixCanBeRemoved(Furniture __instance, ref Farmer __0, ref bool __result)
    {
        try
        {
            if (!ModEntry.Config.Enabled
                || !__result
                || !__instance.furniture_type.Value.Equals(Furniture.rug)
                || __0.currentLocation is not GameLocation currentLocation)
            {
                return;
            }

            Rectangle bounds = __instance.boundingBox.Value;
            int tileX = bounds.X / 64;
            int tileY = bounds.Y / 64;
#if DEBUG
            ModEntry.ModMonitor.Log($"Checking rug: {bounds.X / 64f}, {bounds.Y / 64f}, W/H {bounds.Width / 64f}/{bounds.Height / 64f}", LogLevel.Debug);
#endif
            for (int x = 0; x < bounds.Width / 64; x++)
            {
                for (int y = 0; y < bounds.Height / 64; y++)
                {
                    if (!currentLocation.isTileLocationTotallyClearAndPlaceable(x + tileX, y + tileY))
                    {
                        Game1.showRedMessage(I18n.RugRemovalMessage());
                        __result = false;
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into issues with postfix for Furniture::CanBeRemoved for {__instance.Name}\n\n{ex}", LogLevel.Error);
        }
    }

#if DEBUG
    [SuppressMessage("StyleCop", "SA1313", Justification = "Style prefered by Harmony")]
    private static bool PrefixCanBePlacedHere(Furniture __instance, GameLocation __0, Vector2 tile, ref bool __result)
    {
        try
        {
            if (!ModEntry.Config.Enabled || !ModEntry.Config.CanPlaceRugsUnder
                || !__instance.furniture_type.Value.Equals(Furniture.rug)
                || __instance.placementRestriction != 0 // someone requested a custom placement restriction, respect that.
                )
            {
                return true;
            }
            Rectangle bounds = __instance.boundingBox.Value;
            bool okaytoplace = true;
            for (int x = 0; x < bounds.Width / 64; x++)
            {
                for (int y = 0; y < bounds.Height / 64; y++)
                {
                    // check for large terrain+terrain, refuse placement.
                    // check for is placeable everywhere, and if the thing that's blocking placement is an
                    // another furniture item, I'm still okay to place.
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into errors in PrefixCanBePlacedHere for {__instance.Name} at {__0.NameOrUniqueName} ({tile.X}, {tile.Y})\n\n{ex}", LogLevel.Error);
        }
        return true;
    }
#endif

    /// <summary>
    /// Prefix to prevent objects from accidentally being removed from tables.
    /// </summary>
    /// <param name="__instance">The table.</param>
    /// <param name="who">The farmer who clicked.</param>
    /// <param name="__result">The result of the original function.</param>
    /// <returns>Return true to continue to the original function, false otherwise.</returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Furniture.clicked))]
    [SuppressMessage("StyleCop", "SA1313", Justification = "Style prefered by Harmony")]
    private static bool PrefixClicked(Furniture __instance, Farmer who, ref bool __result)
    {
        try
        {
            if (!ModEntry.Config.Enabled
                || !ModEntry.Config.PreventRemovalFromTable
                || ModEntry.Config.FurniturePlacementKey.IsDown()
                || __instance.furniture_type.Value != Furniture.table)
            {
                return true;
            }
            else
            {
                Game1.showRedMessage(I18n.TableRemovalMessage(keybind: ModEntry.Config.FurniturePlacementKey));
                __result = false;
                return false;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into errors preventing removal of item from table for {who.Name}\n\n{ex}", LogLevel.Error);
            return true;
        }
    }
}