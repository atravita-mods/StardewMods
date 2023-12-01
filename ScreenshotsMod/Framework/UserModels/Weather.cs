namespace ScreenshotsMod.Framework.UserModels;

/// <summary>
/// Represents weather conditions that matter for screenshots.
/// </summary>
[Flags]
public enum Weather
{
    /// <summary>
    /// The weather should be considered a rainy weather.
    /// </summary>
    Rainy = 0b1,

    /// <summary>
    /// The weather should be considered a sunny weather.
    /// </summary>
    Sunny = 0b10,

    /// <summary>
    /// Either sunny or rainy weathers.
    /// </summary>
    Any = Rainy | Sunny,
}
