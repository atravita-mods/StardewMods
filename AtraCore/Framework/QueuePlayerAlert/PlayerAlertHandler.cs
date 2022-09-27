using CommunityToolkit.Diagnostics;
using StardewModdingAPI.Utilities;

namespace AtraCore.Framework.QueuePlayerAlert;

/// <summary>
/// Handles queuing alerts to the player.
/// </summary>
public static class PlayerAlertHandler
{
    private static readonly PerScreen<Queue<HUDMessage>> QueuedMessages = new(() => new());

    /// <summary>
    /// Queues up a HUD message.
    /// </summary>
    /// <param name="message">Message to queue.</param>
    public static void AddMessage(HUDMessage message)
    {
        Guard.IsNotNull(message);

        QueuedMessages.Value.Enqueue(message);
    }

    /// <summary>
    /// Called every ten-in game minutes - loads up to three messages to the player.
    /// </summary>
    internal static void DisplayFromQueue()
    {
        int i = 0;
        while (++i < 3 && QueuedMessages.Value.TryDequeue(out HUDMessage? message))
        {
            Game1.addHUDMessage(message);
        }
    }
}
