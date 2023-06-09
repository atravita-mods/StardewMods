// Ignore Spelling: Dressup Bobbers

namespace DresserMiniMenu.Framework;

/// <summary>
/// The config class for this mod.
/// </summary>
public sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether dressers should allow weapons.
    /// </summary>
    public bool DressersAllowWeapons { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether dressers should allow tackle.
    /// </summary>
    public bool DressersAllowBobbers { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the little mini farmer dress up menu should be drawn.
    /// </summary>
    public bool DresserDressup { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not to draw in the hair selection arrows.
    /// </summary>
    public bool HairArrows { get; set; } = true;
}
