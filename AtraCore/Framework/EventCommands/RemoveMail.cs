using AtraShared.Utils.Extensions;

using StardewValley.Delegates;

namespace AtraCore.Framework.EventCommands;

/// <summary>
/// Removes the following mail.
/// </summary>
internal static class RemoveMail
{
    /// <inheritdoc cref="EventCommandDelegate"/>
    internal static void RemoveMailCommand(Event @event, string[] args, EventContext context)
    {
        if (args.Length < 2)
        {
            @event.LogCommandErrorAndSkip(args, "Event command requires at least one mail flag to remove!");
            return;
        }

        try
        {
            for (int i = 1; i < args.Length; i++)
            {
                string mailflag = args[i];
                if (Game1.player.mailReceived.Remove(mailflag))
                {
                    ModEntry.ModMonitor.LogIfVerbose($"Removing mail flag {mailflag}");
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("trying to remove mail", ex);
        }

        @event.CurrentCommand++;
    }
}