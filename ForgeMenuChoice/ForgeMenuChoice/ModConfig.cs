using StardewModdingAPI.Utilities;

namespace ForgeMenuChoice;

/// <summary>
/// Defines enums on how to show the tooltips.
/// </summary>
public enum TooltipBehavior
{
    /// <summary>
    /// Tooltips should always be enabled.
    /// </summary>
    On,

    /// <summary>
    /// Tooltips should always be disabled.
    /// </summary>
    Off,

    /// <summary>
    /// Tooltips will only be enabled after the player discovers a certain journal scrap.
    /// </summary>
    Immersive,
}

#pragma warning disable SA1623 // Property summary documentation should match accessors. Reviewed.

/// <summary>
/// Configuration class for this mod.
/// </summary>
internal sealed class ModConfig
{
    /// <summary>
    /// Whether or not to show tooltips.
    /// </summary>
    public TooltipBehavior TooltipBehavior { get; set; } = TooltipBehavior.Immersive;

    /// <summary>
    /// Whether to enable automatic generation of tooltips from Journal Scrap 9.
    /// </summary>
    public bool EnableTooltipAutogeneration { get; set; } = true;

    /// <summary>
    /// Gets a button that refers to clicking leftward.
    /// </summary>
    public KeybindList LeftArrow { get; set; } = KeybindList.Parse("LeftShoulder, Left");

    /// <summary>
    /// Gets a button that refers to clicking rightwards.
    /// </summary>
    public KeybindList RightArrow { get; set; } = KeybindList.Parse("RightShoulder, Right");

    public bool OverrideInnateEnchantments { get; set; } = true;
}
#pragma warning restore SA1623 // Property summary documentation should match accessors