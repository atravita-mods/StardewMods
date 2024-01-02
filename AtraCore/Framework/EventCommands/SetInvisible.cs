namespace AtraCore.Framework.EventCommands;

using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.Caches;

using StardewModdingAPI.Events;

using StardewValley.Delegates;

/// <summary>
/// Used to set an NPC invisible for a specific number of days.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SetInvisible"/> class.
/// </remarks>
/// <param name="multiplayer">SMAPI's multiplayer helper.</param>
/// <param name="uniqueID">This mod's uniqueID.</param>
internal sealed class SetInvisible(IMultiplayerHelper multiplayer, string uniqueID)
{
    internal const string RequestSetInvisible = "RequestSetInvisible";

    private const char Sep = 'Ω';

    private string UniqueID { get; init; } = string.Intern(uniqueID);

    /// <inheritdoc cref="EventCommandDelegate"/>
    internal void ApplyInvisibility(Event @event, string[] args, EventContext context)
    {
        // validate
        if (args.Length is not 2 or 3)
        {
            @event.LogCommandErrorAndSkip(args, "Event command expects two arguments: the NPC's internal name and an optional number for the number of days.");
            return;
        }
        if (NPCCache.GetByVillagerName(args[1], searchTheater: true) is not NPC npc)
        {
            @event.LogCommandErrorAndSkip(args, $"Could not find NPC by name {args[1]}");
            return;
        }

        int days = 1;
        if (args.Length == 3 && !int.TryParse(args[2], out days))
        {
            @event.LogCommandErrorAndSkip(args, $"Expected argument 2 (days) to be an integer");
            return;
        }

        npc.IsInvisible = true;

        if (Context.IsMainPlayer)
        {
            ModEntry.ModMonitor.Log($"Setting {npc.Name} invisible for {days} days.");
            npc.daysUntilNotInvisible = days;
        }
        else
        {
            ModEntry.ModMonitor.Log($"Requesting {npc.Name} to be set invisible for {days} days.");
            multiplayer.SendMessage(
                message: $"{npc.Name}{Sep}{days}",
                messageType: RequestSetInvisible,
                modIDs: [this.UniqueID],
                playerIDs: [Game1.MasterPlayer.UniqueMultiplayerID]);
        }

        @event.CurrentCommand++;
    }

    /// <summary>
    /// Processes a request to set an NPC invisible.
    /// </summary>
    /// <param name="e">event args.</param>
    internal void ProcessSetInvisibleRequest(ModMessageReceivedEventArgs e)
    {
        if (!Context.IsMainPlayer)
        {
            return;
        }

        string message = e.ReadAs<string>();

        if (!message.TrySplitOnce(Sep, out ReadOnlySpan<char> first, out ReadOnlySpan<char> second))
        {
            ModEntry.ModMonitor.Log($"Failed to parse message: {message} while handling invisible request.", LogLevel.Warn);
            return;
        }

        if (NPCCache.GetByVillagerName(first.ToString(), searchTheater: true) is not NPC npc)
        {
            ModEntry.ModMonitor.Log($"Failed to find NPC of name {first.ToString()} while handling invisible request.", LogLevel.Warn);
            return;
        }

        if (!int.TryParse(second, out int days) || days < 0)
        {
            ModEntry.ModMonitor.Log($"Failed to parse invisible request {message} as valid number of days.", LogLevel.Warn);
            return;
        }

        ModEntry.ModMonitor.Log($"Setting days until not invisible to {days} for {npc.Name} by multiplayer request.");
        npc.daysUntilNotInvisible = days;
    }
}
