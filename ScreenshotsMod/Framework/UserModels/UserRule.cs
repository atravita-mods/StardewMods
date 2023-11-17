namespace ScreenshotsMod.Framework.UserModels;

public sealed class UserRule
{
    private string path;

    public UserTrigger[] Triggers { get; set; } = [new()];

    public string Path
    {
        get => this.path;
        [MemberNotNull(nameof(this.path))]
        set => this.path = FileNameParser.SanitizePath(value);
    }

    public float Scale { get; set; } = 0.25f;

    public UserRule()
    {
        this.Path = FileNameParser.DEFAULT_FILENAME;
    }
}
