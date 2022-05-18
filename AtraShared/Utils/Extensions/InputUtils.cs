namespace AtraShared.Utils.Extensions;

internal static class InputUtils
{
    internal static void SurpressClickInput(this IInputHelper inputHelper)
    {
        inputHelper.Suppress(Game1.options.actionButton[0].ToSButton());
        inputHelper.Suppress(Game1.options.useToolButton[0].ToSButton());
    }

}