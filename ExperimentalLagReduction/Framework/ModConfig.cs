using AtraShared.Integrations.GMCMAttributes;

namespace ExperimentalLagReduction.Framework;

/// <summary>
/// The config class for this mod.
/// </summary>
internal sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether or not animated sprites should, at best as possible, be forced to use lazy loads.
    /// </summary>
    [GMCMDefaultIgnore]
    public bool ForceLazyTextureLoad { get; set; } =
        #if DEBUG
            true;
        #else
            false;
        #endif

    /// <summary>
    /// Gets or sets a value indicating whether or not to use this mod's scheduler.
    /// </summary>
    [GMCMSection("Scheduler", 0)]
    public bool UseAlternativeScheduler { get; set; } = true;

    [GMCMSection("Scheduler", 0)]
    public bool AllowModAddedDoors { get; set; } = true;

    [GMCMSection("Scheduler", 0)]
    public bool AllowPartialPaths { get; set; } = true;

    [GMCMSection("GiftTastes", 10)]
    public bool OverrideGiftTastes { get; set; } = true;

    [GMCMSection("GiftTastes", 10)]
    public bool UseGiftTastesCache { get; set; } = true;
}
