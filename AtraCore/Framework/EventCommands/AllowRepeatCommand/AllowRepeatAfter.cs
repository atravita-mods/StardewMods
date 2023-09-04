using AtraCore.Interfaces;

using Microsoft.Xna.Framework;

namespace AtraCore.Framework.EventCommands.AllowRepeatCommand;

/// <summary>
/// Allows an event to repeat after X days.
/// </summary>
internal sealed class AllowRepeatAfter : IEventCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AllowRepeatAfter"/> class.
    /// </summary>
    /// <param name="name">Name of the command.</param>
    /// <param name="monitor">Monitor to use.</param>
    public AllowRepeatAfter(string name, IMonitor monitor)
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
        if (args.Length < 2)
        {
            error = "Expected at least a single argument.";
            return false;
        }
        for (int i = 1; i < args.Length; i++)
        {
            if (!int.TryParse(args[i], out _))
            {
                error = $"Expected an integer argument, not {args[i]}";
                return false;
            }
        }

        error = null;
        return true;
    }

    /// <inheritdoc />
    public bool Apply(Event @event, GameLocation location, GameTime time, string[] args, out string? error)
    {
        if (args.Length < 2)
        {
            error = "Expected at least one argument";
            return true;
        }

        if (!int.TryParse(args[^1], out int days) || days < 0)
        {
            error = "Expected a nonnegative, integer number of days";
            return true;
        }

        if (args.Length == 2)
        {
            AllowRepeatAfterHandler.Add(@event.id, days);
        }
        else
        {
            foreach (string id in new ArraySegment<string>(args, 1, args.Length - 2))
            {
                AllowRepeatAfterHandler.Add(id, days);
            }
        }

        error = null;
        return true;
    }
}
