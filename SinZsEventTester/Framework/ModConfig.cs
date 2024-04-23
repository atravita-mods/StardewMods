using StardewModdingAPI.Utilities;

namespace SinZsEventTester.Framework;
public sealed class ModConfig
{
    public int EventSpeedRatio { get; set; } = 4;

    public int FastForwardRatio { get; set; } = 7;

    public KeybindList FastForwardKeybind { get; set; } = new(SButton.K);
}
