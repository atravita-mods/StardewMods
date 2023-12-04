using StardewValley.Delegates;
using StardewValley.SpecialOrders;

namespace SpecialOrdersExtended.Niceties;

/// <summary>
/// Adds a specific special order to the player's team.
/// </summary>
internal static class AddSpecialOrderCommand
{
    /// <inheritdoc cref="EventCommandDelegate"/>
    internal static void AddSpecialOrder(Event @event, string[] args, EventContext context)
    {
        if (args.Length is not 2)
        {
            @event.LogCommandErrorAndSkip(args, "Expected only a single argument, the internal name of the order.");
            return;
        }

        SpecialOrder order = SpecialOrder.GetSpecialOrder(args[1], Random.Shared.Next());
        Game1.player.team.specialOrders.Add(order);
        Game1.Multiplayer.globalChatInfoMessage("AcceptedSpecialOrder", Game1.player.Name, order.GetName());
        @event.CurrentCommand++;
    }
}
