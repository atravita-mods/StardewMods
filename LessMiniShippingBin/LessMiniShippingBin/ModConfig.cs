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
    public int MiniShippingCapacity
    {
        get => this.minishippingcapacity;
        set => this.minishippingcapacity = Math.Clamp(value, 9, 48);
    }

    /// <summary>
    /// Gets or sets the capacity of the jumino chest.
    /// </summary>
    public int JuminoCapacity
    {
        get => this.juminocapcity;
        set => this.juminocapcity = Math.Clamp(value, 9, 48);
    }
}