namespace CameraPan.Framework;

/// <summary>
/// Manages console commands for this mod.
/// </summary>
internal static class ConsoleCommands
{
    /// <summary>
    /// Gets a value indicating whether or not the debug target circle should be drawn.
    /// </summary>
    internal static bool DrawMarker { get; private set; } = false;

    /// <summary>
    /// Registers the console commands for this mod.
    /// </summary>
    /// <param name="commandHelper">console command helper.</param>
    internal static void Register(ICommandHelper commandHelper)
    {
        commandHelper.Add("av.camera.debug", "Sets whether or not to draw a circle on the target point for the camera.", ToggleDebug);
    }

    private static void ToggleDebug(string command, string[] args)
    {
        if (args.Length > 1)
        {
            ModEntry.ModMonitor.Log($"Expected at most one argument");
        }

        if (args.Length == 0)
        {
            DrawMarker = !DrawMarker;
        }
    }
}
