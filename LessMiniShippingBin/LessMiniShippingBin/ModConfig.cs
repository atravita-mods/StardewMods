namespace LessMiniShippingBin;

/// <summary>
/// Configuration class for this mod.
/// </summary>
public class ModConfig
{
    private int capacity = 36;

    /// <summary>
    /// Gets or sets capacity of the mini shipping bin.
    /// </summary>
    public int Capacity
    {
        get => this.capacity;
        set => this.capacity = Math.Clamp(value, 9, 48);
    }
}