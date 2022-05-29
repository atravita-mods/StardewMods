namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extension methods on InputHelper.
/// </summary>
internal static class InputUtils
{
    /// <summary>
    /// Asks inputhelp to supresses the two usual click buttons.
    /// </summary>
    /// <param name="inputHelper">Smapi's inputhelper.</param>
    internal static void SurpressClickInput(this IInputHelper inputHelper)
    {
        inputHelper.Suppress(Game1.options.actionButton[0].ToSButton());
        inputHelper.Suppress(Game1.options.useToolButton[0].ToSButton());
    }
}