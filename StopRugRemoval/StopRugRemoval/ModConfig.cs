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

#if DEBUG
    /// <summary>
    /// Gets or sets a value indicating whether whether or not I should be able to place rugs under things.
    /// </summary>
    public bool CanPlaceRugsUnder { get; set; } = true;
#endif

    /// <summary>
    /// Gets or sets a value indicating whether or not bombs should be confirmed.
    /// </summary>
    public bool ConfirmBombs { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether whether or not to prevent the removal of items from a table.
    /// </summary>
    public bool PreventRemovalFromTable { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether planting on rugs should be allowed.
    /// </summary>
    public bool PreventPlantingOnRugs { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether grass should be placed under objects.
    /// </summary>
    public bool PlaceGrassUnder { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether jukeboxes should be playable everywhere.
    /// </summary>
    public bool JukeboxesEverywhere { get; set; } = true;

    /// <summary>
    /// Allows golden coconuts to appear off the island, if you've cracked at least one before.
    /// </summary>
    public bool GoldenCoconutsOffIsland { get; set; } = false;

    /// <summary>
    /// Gets or sets keybind to use to remove an item from a table.
    /// </summary>
    public KeybindList FurniturePlacementKey { get; set; } = KeybindList.Parse("LeftShift + Z");
}
