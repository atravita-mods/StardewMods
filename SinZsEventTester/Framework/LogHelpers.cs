using StardewValley.Logging;

namespace SinZsEventTester.Framework;

internal static class LogHelpers
{

    internal static void Log(IGameLogger? logger, string message)
    {
        if (logger is not null)
        {
            logger.Info(message);
        }
        else
        {
            ModEntry.ModMonitor.Log(message, LogLevel.Debug);
        }
    }

    internal static void Warn(IGameLogger? logger, string message)
    {
        if (logger is not null)
        {
            logger.Warn(message);
        }
        else
        {
            ModEntry.ModMonitor.Log(message, LogLevel.Warn);
        }
    }
}