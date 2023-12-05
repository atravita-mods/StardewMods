using StardewValley.Delegates;

namespace AtraCore.Framework.EventCommands.AllowRepeatCommand;

/// <summary>
/// Allows an event to repeat after X days.
/// </summary>
internal static class AllowRepeatAfter
{

    /// <inheritdoc cref="EventCommandDelegate"/>
    internal static void SetRepeatAfter(Event @event, string[] args, EventContext context)
    {
        if (args.Length < 2)
        {
            @event.LogCommandErrorAndSkip(args, "Expected at least a single argument.");
            return;
        }
        if (!int.TryParse(args[^1], out int days) || days < 0)
        {
            @event.LogCommandErrorAndSkip(args, "Expected a nonnegative, integer number of days");
            return;
        }

        if (args.Length == 2)
        {
            AllowRepeatAfterHandler.Add(@event.id, days);
        }
        else
        {
            foreach (string id in args.AsSpan(1, args.Length - 2))
            {
                AllowRepeatAfterHandler.Add(id, days);
            }
        }

        @event.CurrentCommand++;
    }
}
