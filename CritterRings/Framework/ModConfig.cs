using AtraShared.Integrations.GMCMAttributes;

using StardewModdingAPI.Utilities;

namespace CritterRings.Framework;

/// <summary>
/// The config class for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Backing fields kept near accessors.")]
public sealed class ModConfig
{
    private int critterSpawnMultiplier = 1;

    /// <summary>
    /// Gets or sets a multiplicative factor which determines the number of critters to spawn.
    /// </summary>
    [GMCMRange(1, 5)]
    public int CritterSpawnMultiplier
    {
        get => this.critterSpawnMultiplier;
        set => this.critterSpawnMultiplier = Math.Clamp(value, 1, 5);
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not butterflies should spawn if it's rainy out.
    /// </summary>
    [GMCMSection("ButterflyRing", 0)]
    public bool ButterfliesSpawnInRain { get; set; } = false;

    private int bunnyRingStamina = 10;

    /// <summary>
    /// Gets or sets a value indicating how expensive the bunny ring's dash should be.
    /// </summary>
    [GMCMRange(0, 50)]
    [GMCMSection("BunnyRing", 10)]
    public int BunnyRingStamina
    {
        get => this.bunnyRingStamina;
        set => this.bunnyRingStamina = Math.Clamp(value, 0, 50);
    }

    private int bunnyRingBoost = 3;

    /// <summary>
    /// Gets or sets a value indicating how big of a speed boost the bunny ring's dash should be.
    /// </summary>
    [GMCMRange(1, 10)]
    [GMCMSection("BunnyRing", 10)]
    public int BunnyRingBoost
    {
        get => this.bunnyRingBoost;
        set => this.bunnyRingBoost = Math.Clamp(value, 1, 10);
    }

    /// <summary>
    /// Gets or sets a value indicating which button should be used for the bunny ring's stamina-sprint.
    /// </summary>
    [GMCMSection("BunnyRing", 10)]
    public KeybindList BunnyRingButton { get; set; } = new KeybindList(new(SButton.LeftShift), new(SButton.DPadDown));
}
