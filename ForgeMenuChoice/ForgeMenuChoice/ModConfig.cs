namespace ForgeMenuChoice;

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
public class ModConfig
{
    /// <summary>
    /// Whether to enable automatic generation of tooltips from Journal Scrap 9.
    /// </summary>
    public bool EnableTooltipAutogeneration { get; set; } = true;

    /// <summary>
    /// Whether or not to show tooltips.
    /// </summary>
    public TooltipBehavior TooltipBehavior { get; set; } = TooltipBehavior.Immersive;
}
#pragma warning restore SA1623 // Property summary documentation should match accessors