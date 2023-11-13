namespace ScreenshotsMod.Framework;

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
    public KeybindList Keybind { get; set; } = KeybindList.ForSingle(SButton.Multiply);

    private string keyBindFileName;

    /// <summary>
    /// Gets or sets the (tokenized) file name to save keybind screenshots at.
    /// </summary>
    public string KeyBindFileName
    {
        get => this.keyBindFileName;
        [MemberNotNull(nameof(keyBindFileName))]
        set => this.keyBindFileName = SanitizePath(value);
    }

    public float KeyBindScale { get; set; } = 1f;

    #endregion

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
