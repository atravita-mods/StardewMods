using AtraShared.Utils.Extensions;

using StardewModdingAPI.Events;

using StardewValley.TerrainFeatures;

namespace StopRugRemoval.Framework.Niceties;

/// <summary>
/// Handles flipping crops.
/// </summary>
internal static class CropAndTreeFlipper
{
    /// <summary>
    /// Handles flipping trees and so on.
    /// </summary>
    /// <param name="e">Button Pressed.</param>
    /// <param name="helper">SMAPI's input helper.</param>
    /// <returns>True if something changed, false otherwise.</returns>
    internal static bool OnButtonPressed(ButtonPressedEventArgs e, IInputHelper helper)
    {
        if ((e.Button.IsActionButton() || e.Button.IsUseToolButton())
            && ModEntry.Config.FurniturePlacementKey.IsDown())
        {
            if (Game1.currentLocation.GetHoeDirtAtTile(e.Cursor.Tile) is HoeDirt dirt
                && dirt.crop is Crop crop)
            {
                ModEntry.ModMonitor.VerboseLog($"Flipping crop at {e.Cursor.Tile}");
                crop.flip.Value = !crop.flip.Value;
                helper.Suppress(e.Button);
                return true;
            }
            else if (Game1.currentLocation.terrainFeatures.TryGetValue(e.Cursor.Tile, out TerrainFeature? feature)
                && feature is FruitTree tree)
            {
                ModEntry.ModMonitor.VerboseLog($"Flipping fruit tree at {e.Cursor.Tile}");
                tree.flipped.Value = !tree.flipped.Value;
                helper.Suppress(e.Button);
                return true;
            }
        }
        return false;
    }
}
