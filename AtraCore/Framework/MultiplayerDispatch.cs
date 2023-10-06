using StardewModdingAPI.Events;

namespace AtraCore.Framework;

/// <summary>
/// Handles dispatching multiplayer messages.
/// </summary>
internal static class MultiplayerDispatch
{
    /// <summary>
    /// This mod's unique ID.
    /// </summary>
    internal static string UniqueId { get; private set; } = null!;

    private static Dictionary<string, Action<ModMessageReceivedEventArgs>> _actions = new();

    internal static void Initialize(string uniqueID)
    {
        UniqueId = string.Intern(uniqueID);
    }

    internal static void Register(string type, Action<ModMessageReceivedEventArgs> action)
    {
        if (!_actions.TryAdd(type, action))
        {
            ModEntry.ModMonitor.LogOnce($"Duplicate type {type} for multiplayer dispatch.", LogLevel.Warn);
        }
    }

    internal static void Apply(ModMessageReceivedEventArgs e)
    {
        if (e is not null && e.FromModID == UniqueId
            && _actions.TryGetValue(e.Type, out Action<ModMessageReceivedEventArgs>? action))
        {
            action(e);
        }
    }
}
