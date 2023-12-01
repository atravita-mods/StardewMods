namespace ScreenshotsMod.Framework.UserModels;

/// <summary>
/// A user defined screenshot rule.
/// </summary>
public sealed class UserRule
{
    private string path;

    /// <summary>
    /// Gets or sets the internal names for maps for which this rule should apply.
    /// </summary>
    public string[] Maps { get; set; } = ["Farm"];

    /// <summary>
    /// Gets or sets a list of triggers that may trigger this rule.
    /// </summary>
    public UserTrigger[] Triggers { get; set; } = [new()];

    /// <summary>
    /// Gets or sets the path to save this rule at.
    /// </summary>
    public string Path
    {
        get => this.path;
        [MemberNotNull(nameof(this.path))]
        set => this.path = FileNameParser.SanitizePath(value);
    }

    /// <summary>
    /// Gets or sets the scale to use.
    /// </summary>
    public float Scale { get; set; } = 0.25f;

    /// <summary>
    /// Gets or sets a value indicating whether or not the screenshot should wait until events are over.
    /// </summary>
    public bool DuringEvents { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRule"/> class.
    /// </summary>
    public UserRule()
    {
        this.Path = FileNameParser.DEFAULT_FILENAME;
    }
}
