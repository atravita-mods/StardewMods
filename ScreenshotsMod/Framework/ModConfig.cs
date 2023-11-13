namespace ScreenshotsMod.Framework;

using AtraShared.Integrations.GMCMAttributes;

using ScreenshotsMod.Framework.UserModels;

using StardewModdingAPI.Utilities;

/// <summary>
/// The config class for this mod.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Fields kept near accessors.")]
public sealed class ModConfig
{
    internal const string DEFAULT_FILENAME = @"{{Default}}/{{Save}}/{{Location}}/{{Date}}.png";

    #region general
    public bool ScreenFlash { get; set; } = true;

    public bool AudioCue { get; set; } = true;
    #endregion

    #region keybind

    /// <summary>
    /// Gets or sets the keybind to use to take screenshots.
    /// </summary>
    public KeybindList KeyBind { get; set; } = KeybindList.ForSingle(SButton.Multiply);

    private string keyBindFileName;
    private float keyBindScale = 1f;

    /// <summary>
    /// Gets or sets the (tokenized) file name to save keybind screenshots at.
    /// </summary>
    public string KeyBindFileName
    {
        get => this.keyBindFileName;
        [MemberNotNull(nameof(keyBindFileName))]
        set => this.keyBindFileName = SanitizePath(value);
    }

    /// <summary>
    /// The scale of the image to use for a keybind screenshot.
    /// </summary>
    [GMCMInterval(0.1)]
    [GMCMRange(0.01, 1)]
    public float KeyBindScale
    {
        get => this.keyBindScale;
        set => this.keyBindScale = Math.Clamp(value, 0.01f, 1f);
    }
    #endregion

    public Dictionary<string, UserRule> Rules { get; set; } = new()
    {
        ["Default"] = new()
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ModConfig"/> class.
    /// </summary>
    public ModConfig()
    {
        this.KeyBindFileName = DEFAULT_FILENAME;
    }

    /// <summary>
    /// Sanitizes a given path.
    /// </summary>
    /// <param name="value">Path to sanitize.</param>
    /// <returns>Sanitized (hopefully) path.</returns>
    internal static string SanitizePath(string value)
    {
        var proposed = PathUtilities.NormalizePath(value);
        var ext = Path.GetExtension(proposed);
        if (!ext.Equals(".png", Constants.TargetPlatform is GamePlatform.Linux or GamePlatform.Android ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
        {
            proposed += ".png";
        }
        return proposed;
    }
}
