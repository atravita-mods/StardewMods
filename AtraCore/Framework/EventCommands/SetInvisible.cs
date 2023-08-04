using AtraCore.Framework.Caches;
using AtraCore.Interfaces;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;

namespace AtraCore.Framework.EventCommands;

/// <summary>
/// Used to set an NPC invisible for a specific number of days.
/// </summary>
internal sealed class SetInvisible : IEventCommand
{

    /// <summary>
    /// Initializes a new instance of the <see cref="SetInvisible"/> class.
    /// </summary>
    /// <param name="name">Name of the command.</param>
    /// <param name="monitor">Monitor to use.</param>
    public SetInvisible(string name, IMonitor monitor)
    {
        this.Name = name;
        this.Monitor = monitor;
    }

    /// <inheritdoc />
    public string Name { get; init; }

    /// <inheritdoc />
    public IMonitor Monitor { get; init; }

    /// <inheritdoc />
    public bool Validate(Event @event, GameLocation location, GameTime time, string[] args, out string? error)
    {
        if (args.Length is not 2 or 3)
        {
            error = "Event command expects two arguments: the NPC's internal name and an optional number for the number of days.";
            return false;
        }
        if (NPCCache.GetByVillagerName(args[1], searchTheater: true) is not NPC npc)
        {
            error = $"Could not find NPC by name {args[1]}";
            return false;
        }
        if (args.Length == 3 && !int.TryParse(args[2], out var _))
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
            if (args.Length > 2 && int.TryParse(args[2], out var val) && val > 1)
            {
                days = val;
            }

            ModEntry.ModMonitor.Log($"Setting {npc.Name} invisible for {days} days.");
            npc.IsInvisible = true;
            npc.daysUntilNotInvisible = days;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("trying to set npc invisible", ex);
        }

        error = null;
        return true;
    }
}
