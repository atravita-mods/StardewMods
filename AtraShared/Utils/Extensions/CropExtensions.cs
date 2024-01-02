namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extension methods for crops.
/// </summary>
public static class CropExtensions
{
    /// <summary>
    /// Copied from the game - gets if a crop is harvestable.
    /// </summary>
    /// <param name="crop">Crop in question.</param>
    /// <returns>True if harvestable.</returns>
    public static bool IsActuallyFullyGrown(this Crop? crop)
        => crop is not null && crop.currentPhase.Value >= crop.phaseDays.Count - 1 && (!crop.fullyGrown.Value || crop.dayOfCurrentPhase.Value <= 0);
}
