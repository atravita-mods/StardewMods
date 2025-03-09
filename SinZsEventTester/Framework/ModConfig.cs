using StardewModdingAPI.Utilities;

namespace SinZsEventTester.Framework;

/// <summary>
/// The config class for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Fields kept near accessors.")]
public sealed class ModConfig
{
    private int eventSpeedRatio = 4;

    public int EventSpeedRatio
    {
        get => this.eventSpeedRatio;
        set => this.eventSpeedRatio = Math.Max(1, value);
    }

    private int fastForwardRatio = 7;

    /// <summary>
    /// Gets or sets the amount to fast forward.
    /// </summary>
    public int FastForwardRatio
    {
        get => this.fastForwardRatio;
        set => this.fastForwardRatio = Math.Max(1, value);
    }

    /// <summary>
    /// Gets or sets the button used to trigger fast forwards.
    /// </summary>
    public KeybindList FastForwardKeybind { get; set; } = new(SButton.End);

    public bool AllowCheats { get; set; } = true;

    public bool SkipDialogue { get; set; } = true;
}
