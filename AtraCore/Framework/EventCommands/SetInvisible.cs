using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.Caches;
using AtraCore.Interfaces;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

namespace AtraCore.Framework.EventCommands;

/// <summary>
/// Used to set an NPC invisible for a specific number of days.
/// </summary>
internal sealed class SetInvisible : IEventCommand
{
    private const string RequestSetInvisible = "RequestSetInvisible";

    private const char Sep = 'Ω';

    /// <summary>
    /// Initializes a new instance of the <see cref="SetInvisible"/> class.
    /// </summary>
    /// <param name="name">Name of the command.</param>
    /// <param name="monitor">Monitor to use.</param>
    /// <param name="multiplayer">SMAPI's multiplayer helper.</param>
    /// <param name="uniqueID">This mod's uniqueID.</param>
    public SetInvisible(string name, IMonitor monitor, IMultiplayerHelper multiplayer, string uniqueID)
    {
        this.Name = name;
        this.Monitor = monitor;
        this.Multiplayer = multiplayer;
        this.UniqueID = string.Intern(uniqueID);
    }

    /// <inheritdoc />
    public string Name { get; init; }

    /// <inheritdoc />
    public IMonitor Monitor { get; init; }

    private IMultiplayerHelper Multiplayer { get; init; }

    private string UniqueID { get; init; }

    /// <inheritdoc />
    public bool Validate(Event @event, GameLocation location, GameTime time, string[] args, out string? error)
    {
        if (args.Length is not 2 or 3)
        {
            error = "Event command expects two arguments: the NPC's internal name and an optional number for the number of days.";
            return false;
        }
        if (NPCCache.GetByVillagerName(args[1], searchTheater: true) is not NPC)
        {
            error = $"Could not find NPC by name {args[1]}";
            return false;
        }
        if (args.Length == 3 && !int.TryParse(args[2], out int _))
        {
            error = $"Expected argument 2 (days) to be an integer";
            return false;
        }

        error = null;
        return true;
    }

    /// <inheritdoc />
    public bool Apply(Event @event, GameLocation location, GameTime time, string[] args, out string? error)
    {
        try
        {
            NPC? npc = NPCCache.GetByVillagerName(args[1], searchTheater: true);
            if (npc is null)
            {
                error = $"Could not find NPC by name {args[1]}";
                return true;
            }

            int days = 1;
            if (args.Length > 2 && int.TryParse(args[2], out int val) && val > 1)
            {
                days = val;
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
                this.Multiplayer.SendMessage(
                    message: $"{npc.Name}{Sep}{days}",
                    messageType: RequestSetInvisible,
                    modIDs: new[] { this.UniqueID },
                    playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("trying to set npc invisible", ex);
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Processes a request to set an NPC invisible.
    /// </summary>
    /// <param name="sender">smapi</param>
    /// <param name="e">event args.</param>
    internal void ProcessSetInvisibleRequest(object? sender, ModMessageReceivedEventArgs e)
    {
        if (!Context.IsMainPlayer || e.FromModID != this.UniqueID || e.Type != RequestSetInvisible)
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
