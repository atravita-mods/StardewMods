using AtraShared.Integrations.GMCMAttributes;

namespace ExperimentalLagReduction.Framework;

/// <summary>
/// The config class for this mod.
/// </summary>
internal sealed class ModConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether or not to cull out of bound draws.
    /// </summary>
    public bool CullDraws { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not partial paths should be allowed.
    /// </summary>
    [GMCMSection("Scheduler", 0)]
    public bool AllowPartialPaths { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to pre-populate the cache for commonly-used areas.
    /// </summary>
    [GMCMSection("Scheduler", 0)]
    public bool PrePopulateCache { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not to use my gift taste code.
    /// </summary>
    [GMCMSection("GiftTastes", 10)]
    public bool OverrideGiftTastes { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not to use a cache for gift tastes.
    /// </summary>
    [GMCMSection("GiftTastes", 10)]
    public bool UseGiftTastesCache { get; set; } = true;
}
