using AtraCore;
using AtraCore.Interfaces;
using AtraCore.Utilities;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;

namespace SpecialOrdersExtended.Niceties;

/// <summary>
/// Adds a specific special order to the player's team.
/// </summary>
internal sealed class AddSpecialOrderCommand : IEventCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddSpecialOrderCommand"/> class.
    /// </summary>
    /// <param name="name">Name of the command.</param>
    /// <param name="monitor">Monitor instance.</param>
    public AddSpecialOrderCommand(string name, IMonitor monitor)
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
            error = "Expected at most two arguments, the internal name of the order, and an optional boolean `duplicate` argument.";
            return false;
        }
        if (args.Length == 3 && !bool.TryParse(args[2], out _))
        {
            error = "Expected argument 2 `duplicate` to be a boolean.";
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
            // if duplicates allowed, or if no duplicate found.
            if ((args.Length == 3 && bool.TryParse(args[2], out bool val) && val)
                || !Game1.player.team.specialOrders.Any(order => order.questKey.Value == args[1])
            {
                SpecialOrder order = SpecialOrder.GetSpecialOrder(args[1], Singletons.Random.Next());
                Game1.player.team.specialOrders.Add(order);
                MultiplayerHelpers.GetMultiplayer().globalChatInfoMessage("AcceptedSpecialOrder", Game1.player.Name, order.GetName());
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding a special order", ex);
        }

        error = null;
        return true;
    }
}
