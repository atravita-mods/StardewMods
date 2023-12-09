using AtraShared.Integrations.GMCMAttributes;

using HighlightEmptyMachines.Legacy;

using Microsoft.Xna.Framework;

using StardewValley.GameData.Machines;

namespace HighlightEmptyMachines;

/// <summary>
/// The config class for this mod.
/// </summary>
public sealed class ModConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModConfig"/> class.
    /// </summary>
    public ModConfig()
    {
        // Set invalid to be just a little transparent.
        Color invalid = Color.Gray;
        invalid.A = 200;
        this.InvalidColor = invalid;
    }

    /// <summary>
    /// Gets or sets the color to color empty machines.
    /// </summary>
    [GMCMDefaultColor(255, 0, 0, 255)]
    public Color EmptyColor { get; set; } = Color.Red;

    /// <summary>
    /// Gets or sets the color to color invalid machines.
    /// </summary>
    [GMCMDefaultColor(128, 128, 128, 200)]
    public Color InvalidColor { get; set; } = Color.Gray;

    /// <summary>
    /// Gets or sets a mapping that sets whether coloration of vanilla machines should be enabled.
    /// Mapping is between qualified item ids->whether or not it should be enabled.
    /// </summary>
    [GMCMDefaultIgnore]
    public Dictionary<string, bool> VanillaMachines { get; set; } = [];

    /// <summary>
    /// Gets or sets a mapping that sets whether coloration of PFM machines should be enabled.
    /// </summary>
    [GMCMDefaultIgnore]
    public Dictionary<string, bool> ProducerFrameworkModMachines { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether or not machine pulsing should be disabled.
    /// </summary>
    public bool DisablePulsing { get; set; } = false;

    internal void Populate()
    {
        foreach ((string machine, MachineData data) in DataLoader.Machines(Game1.content))
        {
            this.VanillaMachines.TryAdd(machine, !data.IsIncubator);
        }
    }
}