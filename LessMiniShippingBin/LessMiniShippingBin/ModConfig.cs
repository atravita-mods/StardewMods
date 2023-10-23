using AtraShared.Integrations.GMCMAttributes;

namespace LessMiniShippingBin;

/// <summary>
/// Configuration class for this mod.
/// </summary>
internal sealed class ModConfig
{
    private int minishippingcapacity = 36;
    private int juminocapcity = 9;

    /// <summary>
    /// Gets or sets capacity of the mini shipping bin.
    /// </summary>
    [GMCMInterval(9)]
    [GMCMRange(9, 48)]
    public int MiniShippingCapacity
    {
        get => this.minishippingcapacity;
        set => this.minishippingcapacity = Math.Clamp(value, 9, 48);
    }

    /// <summary>
    /// Gets or sets the capacity of the jumino chest.
    /// </summary>
    [GMCMInterval(9)]
    [GMCMRange(9, 48)]
    public int JuminoCapacity
    {
        get => this.juminocapcity;
        set => this.juminocapcity = Math.Clamp(value, 9, 48);
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not to draw the first held item of a chest in front of it.
    /// </summary>
    public bool DrawFirstItem { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not to draw the first held item for a fridge.
    /// </summary>
    public bool DrawFirstItemFridge { get; set; } = false;
}