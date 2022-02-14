using StardewModdingAPI.Utilities;

namespace StopRugRemoval;

/// <summary>
/// Configuration class for this mod.
/// </summary>
public class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether whether or not the entire mod is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether whether or not I should be able to place rugs outside.
    /// </summary>
    public bool CanPlaceRugsOutside { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether whether or not I should be able to place rugs under things.
    /// </summary>
    public bool CanPlaceRugsUnder { get; set; } = true;

    public bool PreventRemovalFromTable { get; set; } = true;

    // Keybind for the place rugs under?
    public KeybindList FurniturePlacementKey { get; set; } = KeybindList.Parse("LeftShift + Z");
}
