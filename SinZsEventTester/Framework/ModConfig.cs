using StardewModdingAPI.Utilities;

namespace SinZsEventTester.Framework;
public sealed class ModConfig
{
    public int EventSpeedRatio { get; set; } = 4;

    private int fastForwardRatio = 7;
    public int FastForwardRatio
    {
        get => this.fastForwardRatio;
        set => this.fastForwardRatio = Math.Max(1, value);
    }

    public KeybindList FastForwardKeybind { get; set; } = new(SButton.K);

    public bool AllowCheats { get; set; } = true;

    public bool SkipDialogue { get; set; } = true;
}
