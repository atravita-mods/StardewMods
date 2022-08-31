using AtraShared.Menuing;
using AtraShared.Utils.Extensions;
using StardewModdingAPI.Events;
using StardewValley.TerrainFeatures;

namespace StopRugRemoval.Framework.Niceties;
internal static class CropAndTreeFlipper
{
    internal static void OnButtonPressed(ButtonPressedEventArgs e, IInputHelper helper)
    {
        if ((e.Button.IsActionButton() || e.Button.IsUseToolButton())
            && ModEntry.Config.FurniturePlacementKey.IsDown()
            && MenuingExtensions.IsNormalGameplay())
        {
            if (Game1.currentLocation.GetHoeDirtAtTile(e.Cursor.Tile) is HoeDirt dirt
                && dirt.crop is Crop crop)
            {
                ModEntry.ModMonitor.DebugOnlyLog($"Flipping crop at {e.Cursor.Tile}");
                crop.flip.Value = !crop.flip.Value;
                helper.Suppress(e.Button);
            }
            else if (Game1.currentLocation.terrainFeatures.TryGetValue(e.Cursor.Tile, out var feature)
                && feature is FruitTree tree)
            {
                ModEntry.ModMonitor.DebugLog($"Flipping fruit tree at {e.Cursor.Tile}");
                tree.flipped.Value = !tree.flipped.Value;
                helper.Suppress(e.Button);
            }
        }
    }
}
