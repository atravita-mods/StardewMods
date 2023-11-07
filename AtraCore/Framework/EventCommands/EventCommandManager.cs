using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.ReflectionManager;
using AtraCore.Interfaces;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using CommunityToolkit.Diagnostics;

using HarmonyLib;

using Microsoft.Xna.Framework;

namespace AtraCore.Framework.EventCommands;

/// <summary>
/// Handles event commands.
/// </summary>
// [HarmonyPatch(typeof(Event))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
public static class EventCommandManager
{
    private static readonly Dictionary<string, IEventCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds an event command to the dictionary.
    /// </summary>
    /// <param name="command">Command to add.</param>
    /// <returns>True if successfully added, false otherwise.</returns>
    public static bool Add(IEventCommand command)
    {
        Guard.IsNotNull(command);
        Guard.IsNotNullOrWhiteSpace(command.Name);
        Guard.IsNotNull(command.Monitor);
        ModEntry.ModMonitor.DebugOnlyLog($"Adding event command {command.Name}.");

        return _commands.TryAdd(string.Intern(command.Name) ?? command.Name, command);
    }

    [HarmonyPriority(Priority.High)]
    [HarmonyPatch(nameof(Event.tryEventCommand))]
    private static bool Prefix(Event __instance, GameLocation location, GameTime time, string[] split)
    {
        try
        {
            if (split.Length == 0)
            {
                return true;
            }
            else if (_commands.TryGetValue(split[0], out IEventCommand? handler))
            {
                if (!handler.Validate(__instance, location, time, split, out string? error))
                {
                    handler.Monitor.Log($"Custom command {split[0]} cannot process {string.Join(' ', split.SkipToSegment(1))}", LogLevel.Warn);
                    if (!string.IsNullOrEmpty(error))
                    {
                        handler.Monitor.Log($"Error returned: {error}", LogLevel.Warn);
                    }

                    __instance.CurrentCommand++;
                }
                else
                {
                    if (handler.Apply(__instance, location, time, split, out string? applyError))
                    {
                        __instance.CurrentCommand++;
                    }

                    if (!string.IsNullOrEmpty(applyError))
                    {
                        handler.Monitor.Log($"Error returned: {applyError}", LogLevel.Warn);
                    }
                }

                return false;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("execute a custom event command", ex);
            __instance.CurrentCommand++;
        }

        return true;
    }
}
