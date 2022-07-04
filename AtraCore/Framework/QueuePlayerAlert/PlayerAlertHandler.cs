using StardewModdingAPI.Utilities;

namespace AtraCore.Framework.QueuePlayerAlert;
public static class PlayerAlertHandler
{
    private static PerScreen<Queue<HUDMessage>> queuedMessages = new(() => new());

    public static void AddMessage(HUDMessage message)
    {
        queuedMessages.Value.Enqueue(message);
    }

    /// <summary>
    /// Called every ten-in game minutes - loads up to five messages to the player.
    /// </summary>
    internal static void DisplayFromQueue()
    {
        int i = 0;
        while (queuedMessages.Value.TryDequeue(out HUDMessage? message) && i++ < 4)
        {
            Game1.addHUDMessage(message);
        }
    }
}
