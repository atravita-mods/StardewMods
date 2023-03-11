using AtraShared.Integrations.GMCMAttributes;

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
    public bool ButterfliesSpawnInRain { get; set; } = false;

    private int bunnyRingStamina = 20;

    /// <summary>
    /// Gets or sets a value indicating how expensive the bunny ring's dash should be.
    /// </summary>
    [GMCMRange(0, 50)]
    public int BunnyRingStamina
    {
        get => this.bunnyRingStamina;
        set => this.bunnyRingStamina = Math.Clamp(value, 0, 50);
    }
}
