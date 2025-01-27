// Ignore Spelling: Jumino

namespace LessMiniShippingBin;

/// <summary>
/// Configuration class for this mod.
/// </summary>
internal sealed class ModConfig
{
    internal const int MAX_CHEST_SIZE = 120;
    internal const int MIN_CHEST_SIZE = 9;

    private int minishippingcapacity = 36;
    private int juminocapcity = 9;
    private int smallChestCapacity = 36;
    private int bigChestCapacity = 70;
    private int fridgeCapacity = 36;

    /// <summary>
    /// Gets or sets capacity of the mini shipping bin.
    /// </summary>
    public int MiniShippingCapacity
    {
        get => this.minishippingcapacity;
        set => this.minishippingcapacity = ClampValue(value);
    }

    /// <summary>
    /// Gets or sets the capacity of the jumino chest.
    /// </summary>
    public int JuminoCapacity
    {
        get => this.juminocapcity;
        set => this.juminocapcity = ClampValue(value);
    }

    /// <summary>
    /// Gets or sets the capacity of small chests.
    /// </summary>
    public int SmallChestCapacity
    {
        get => this.smallChestCapacity;
        set => this.smallChestCapacity = ClampValue(value);

    }

    /// <summary>
    /// Gets or sets the capacity of big chests.
    /// </summary>
    public int BigChestCapacity
    {
        get => this.bigChestCapacity;
        set => this.bigChestCapacity = ClampValue(value);
    }

    /// <summary>
    /// Gets or sets the capacity of fridges.
    /// </summary>
    public int FridgeCapacity
    {
        get => this.fridgeCapacity;
        set => this.fridgeCapacity = ClampValue(value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not to draw the first held item of a chest in front of it.
    /// </summary>
    public bool DrawFirstItem { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not to draw the first held item for a fridge.
    /// </summary>
    public bool DrawFirstItemFridge { get; set; } = false;

    internal static int ClampValue(int value)
    {
        var clamped = Math.Clamp(value, MIN_CHEST_SIZE, MAX_CHEST_SIZE);
        var rows = clamped < 70 ? 3 : 5;
        return clamped - (clamped % rows);
    }
}