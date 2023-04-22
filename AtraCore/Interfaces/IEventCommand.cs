using Microsoft.Xna.Framework;

namespace AtraCore.Interfaces;

/// <summary>
/// An interface for event commands.
/// </summary>
public interface IEventCommand
{

    /// <summary>
    /// Gets the name of the command. For compatibility, prefix with your name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the monitor instance to use.
    /// </summary>
    public IMonitor Monitor { get; init; }

    /// <summary>
    /// Checks if the event command is formatted correctly.
    /// </summary>
    /// <param name="event">Event instance.</param>
    /// <param name="location">Location the event is at.</param>
    /// <param name="time">Good question.</param>
    /// <param name="args">Event args.</param>
    /// <param name="error">Error message, if applicable.</param>
    /// <returns>True if the command should be run, false if there is a formatting issue.</returns>
    public bool Validate(Event @event, GameLocation location, GameTime time, string[] args, out string? error);

    /// <summary>
    /// Tries to run the event command.
    /// </summary>
    /// <param name="event">Event instance.</param>
    /// <param name="location">Location the event is at.</param>
    /// <param name="time">Good question.</param>
    /// <param name="args">Event args.</param>
    /// <param name="error">Error message, if applicable.</param>
    /// <returns>True if we should advance to the next command, false otherwise.</returns>
    public bool Apply(Event @event, GameLocation location, GameTime time, string[] args, out string? error);
}
