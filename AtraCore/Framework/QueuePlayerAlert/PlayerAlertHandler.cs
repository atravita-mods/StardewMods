using CommunityToolkit.Diagnostics;
using StardewModdingAPI.Utilities;

namespace AtraCore.Framework.QueuePlayerAlert;

/// <summary>
/// Handles queuing alerts to the player.
/// </summary>
public static class PlayerAlertHandler
{
    private static readonly PerScreen<Queue<(HUDMessage message, string? soundCue)>> QueuedMessages = new(() => new());

    /// <summary>
    /// Queues up a HUD message with no sound.
    /// </summary>
    /// <param name="message">Message to queue.</param>
    public static void AddMessage(HUDMessage message) => AddMessage(message, null);

    /// <summary>
    /// Queues up a HUD message.
    /// </summary>
    /// <param name="message">Message to queue.</param>
    /// <param name="soundCue">The sound cue to play.</param>
    public static void AddMessage(HUDMessage message, string? soundCue)
    {
        Guard.IsNotNull(message);

        QueuedMessages.Value.Enqueue((message, soundCue));
    }

    /// <summary>
    /// Called every ten-in game minutes - loads up to <paramref name="count"/> messages to the player.
    /// </summary>
    /// <param name="count">Number of messages to display.</param>
    internal static void DisplayFromQueue(int count = 3)
    {
        int i = 0;
        while (++i <= count && QueuedMessages.Value.TryDequeue(out (HUDMessage message, string? soundCue) tuple))
        {
            Game1.addHUDMessage(tuple.message);
            if (tuple.soundCue is not null)
            {
                Game1.playSound(tuple.soundCue);
            }
        }
    }

    internal static bool HasMessages() => QueuedMessages.Value.Count > 0;
}
