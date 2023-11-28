namespace ScreenshotsMod.Framework.ModModels;
internal sealed class ProcessedRule(string Path, float Scale, ProcessedTrigger[] Triggers)
{
    private bool triggered = false;

    internal void Reset() => this.triggered = false;

    internal (string path, float scale)? GetScreenshot()
    {
        foreach (var trigger in Triggers)
        {
            if (trigger.Check())
            {
                this.triggered = true;
                return (Path, Scale);
            }
        }
        return null;
    }
}
