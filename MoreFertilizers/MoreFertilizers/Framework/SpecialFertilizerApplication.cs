using AtraBase.Toolkit;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

using StardewValley.Buildings;

namespace MoreFertilizers.Framework;

/// <summary>
/// Handles applying special fertilizers.
/// </summary>
[HarmonyPatch(typeof(Utility))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class SpecialFertilizerApplication
{
    private const int PLACEMENTRADIUS = 2;

    private static readonly CanPlaceHandler PlaceHandler = new();

    /// <summary>
    /// Handles applying a fertilizer from an input button press.
    /// </summary>
    /// <param name="e">Button press event arguments.</param>
    /// <param name="helper">SMAPI's input helper.</param>
    internal static void ApplyFertilizer(ButtonPressedEventArgs e, IInputHelper helper)
    {
        if (Game1.player.ActiveObject?.bigCraftable?.Value != false || Game1.player.ActiveObject.GetType() != typeof(SObject))
        {
            return;
        }

        SObject obj = Game1.player.ActiveObject;

        Vector2 placementtile = Utility.withinRadiusOfPlayer(((int)e.Cursor.Tile.X * 64) + 32, ((int)e.Cursor.Tile.Y * 64) + 32, PLACEMENTRADIUS, Game1.player)
                                    ? e.Cursor.Tile
                                    : e.Cursor.GrabTile;

        // HACK move the tile further from the player if they're controller.
        if ((obj.ParentSheetIndex == ModEntry.FishFoodID || obj.ParentSheetIndex == ModEntry.DeluxeFishFoodID) &&
            !Game1.currentLocation.isWaterTile((int)placementtile.X, (int)placementtile.Y))
        {
            placementtile = Game1.player.FacingDirection switch
            {
                Game1.up => Game1.player.Tile - new Vector2(0, 3),
                Game1.down => Game1.player.Tile + new Vector2(0, 3),
                Game1.left => Game1.player.Tile - new Vector2(3, 0),
                _ => Game1.player.Tile + new Vector2(3, 0)
            };
        }

        ModEntry.ModMonitor.DebugOnlyLog($"Checking tile {placementtile}");

        if (!PlaceHandler.CanPlaceFertilizer(obj, Game1.currentLocation, placementtile, true))
        {
            return;
        }

        // Handle the graphics for the special case of tossing the fish food fertilizer.
        if (obj.ParentSheetIndex == ModEntry.FishFoodID || obj.ParentSheetIndex == ModEntry.DeluxeFishFoodID || obj.ParentSheetIndex == ModEntry.DomesticatedFishFoodID)
        {
            Vector2 placementpixel = (placementtile * 64f) + new Vector2(32f, 32f);
            if (obj.ParentSheetIndex == ModEntry.DomesticatedFishFoodID)
            {
                foreach (Building b in Game1.currentLocation.buildings)
                {
                    if (b is FishPond fishPond && b.occupiesTile(placementtile))
                    {
                        placementpixel = fishPond.GetCenterTile() * 64f;
                        break;
                    }
                }
            }
            Game1.player.FaceFarmerTowardsPosition(placementpixel);
            Game1.playSound("throwDownITem");

            Multiplayer? mp = Game1.Multiplayer;
            float time = obj.ParabolicThrowItem(Game1.player.Position - new Vector2(0, 128), placementpixel, mp, Game1.currentLocation);

            GameLocationUtils.DrawWaterSplash(Game1.currentLocation, placementpixel, mp, (int)time);

            DelayedAction.playSoundAfterDelay("waterSlosh", (int)time, Game1.player.currentLocation);
            if (obj.ParentSheetIndex != ModEntry.DomesticatedFishFoodID)
            {
                DelayedAction.functionAfterDelay(
                    static () => Game1.currentLocation.waterColor.Value = ModEntry.Config.WaterOverlayColor,
                    (int)time);
            }
        }

        // The actual placement.
        if (PlaceHandler.TryPlaceFertilizer(obj, Game1.currentLocation, placementtile))
        {
            Game1.player.reduceActiveItemByOne();
            helper.Suppress(e.Button);
            return;
        }
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(nameof(Utility.playerCanPlaceItemHere))]
    private static bool PrefixPlayerCanPlaceItemHere(GameLocation location, Item item, int x, int y, Farmer f, ref bool __result)
    {
        if (!TypeUtils.IsExactlyOfType(item, out SObject? obj) || obj.bigCraftable.Value)
        {
            return true;
        }

        try
        {
            Vector2 tile = new(x / Game1.tileSize, y / Game1.tileSize);
            if (PlaceHandler.CanPlaceFertilizer(obj, location, tile) &&
                Utility.withinRadiusOfPlayer(x, y, PLACEMENTRADIUS, f))
            {
                __result = true;
                return false;
            }
            else if (obj.Category == SObject.fertilizerCategory
                && ModEntry.SpecialFertilizerIDs.Contains(obj.ParentSheetIndex))
            {
                __result = false;
                return false;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("prefixing Utility.playerCanPlaceItemHere", ex);
        }
        return true;
    }
}