// Ignore Spelling: Dressup Bobbers

using AtraBase.Toolkit;

using AtraShared.Integrations.GMCMAttributes;

namespace DresserMiniMenu.Framework;

/// <summary>
/// The config class for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = StyleCopErrorConsts.AccessorsNearFields)]
public sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether dressers should allow weapons.
    /// </summary>
    public bool DressersAllowWeapons { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether dressers should allow bobbers.
    /// </summary>
    public bool DressesAllowTrinkets { get; set; } = true;

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

    /*
    private int haleyStockLimit = 7;

    /// <summary>
    /// Gets or sets a value indicating how many items Haley should have up for loan.
    /// </summary>
    [GMCMRange(0, 64)]
    [GMCMSection("Shop", 0)]
    public int HaleyStockLimit
    {
        get => this.haleyStockLimit;
        set => this.haleyStockLimit = Math.Clamp(value, 0, 64);
    }

    private int haleyHeartsLimit = 8;

    /// <summary>
    /// Gets or sets a value indicating the number of hearts Haley should require before she'll start lending you clothing.
    /// </summary>
    [GMCMRange(0, 14)]
    [GMCMSection("Shop", 0)]
    public int HaleyHeartsLimit
    {
        get => this.haleyHeartsLimit;
        set => this.haleyHeartsLimit = Math.Clamp(value, 0, 14);
    }
    */
}
