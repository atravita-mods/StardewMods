using StardewModdingAPI.Events;

namespace AtraCore.Framework;

/// <summary>
/// Handles dispatching multiplayer messages.
/// </summary>
internal static class MultiplayerDispatch
{
    private static readonly Dictionary<string, Action<ModMessageReceivedEventArgs>> _actions = [];

    /// <summary>
    /// Gets this mod's unique ID.
    /// </summary>
    internal static string UniqueId { get; private set; } = null!;

    /// <summary>
    /// Initializes this class.
    /// </summary>
    /// <param name="uniqueID">The uniqueID of this mod.</param>
    internal static void Initialize(string uniqueID)
    {
        UniqueId = string.Intern(uniqueID);
    }

    /// <summary>
    /// Registers an action to take.
    /// </summary>
    /// <param name="type">The <see cref="ModMessageReceivedEventArgs.Type"/> to handle.</param>
    /// <param name="action">The action to do.</param>
    internal static void Register(string type, Action<ModMessageReceivedEventArgs> action)
    {
        if (!_actions.TryAdd(type, action))
        {
            ModEntry.ModMonitor.LogOnce($"Duplicate type {type} for multiplayer dispatch.", LogLevel.Warn);
        }
    }

    /// <inheritdoc cref="IMultiplayerEvents.ModMessageReceived"/>
    internal static void Apply(ModMessageReceivedEventArgs e)
    {
        if (e is not null && e.FromModID == UniqueId
            && _actions.TryGetValue(e.Type, out Action<ModMessageReceivedEventArgs>? action))
        {
            action(e);
        }
    }
}
