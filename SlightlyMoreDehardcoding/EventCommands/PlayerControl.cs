using HarmonyLib;

using Microsoft.Xna.Framework;

using MiniAtraShared.Extensions;

using StardewModdingAPI.Utilities;

using StardewValley.Triggers;

namespace SlightlyMoreDehardcoding.EventCommands;

internal static class PlayerControl
{
    private const string Key = "atravita.PlayerControl";

    private static PerScreen<HashSet<Point>> _currentControlPoints = new(() => []);

    private static PerScreen<Dictionary<Point, List<string>>> _currentActionPoints = new(() => []);
    private static PerScreen<Dictionary<Point, List<string[]>>> _currentEventCommandPoints = new(() => []);

    internal static void AddActionForTile(Event @event, string[] args, EventContext context)
    {
        if (!!ArgUtility.TryGetPoint(args, 1, out var point, out var error)
            || !ArgUtility.TryGetRemainder(args, 3, out var @action, out error))
        {
            @event.LogCommandErrorAndSkip(args, error);
            return;
        }

        var dictionary = _currentActionPoints.Value;

        if (!dictionary.TryGetValue(point, out List<string>? data))
        {
            dictionary[point] = data = [];
        }

        data.Add(@action);
        @event.CurrentCommand++;
    }

    internal static void AddEventCommandForTile(Event @event, string[] args, EventContext context)
    {
        if (!ArgUtility.TryGetInt(args, 1, out var x, out var error)
            || !ArgUtility.TryGetInt(args, 2, out var y, out error))
        {
            @event.LogCommandErrorAndSkip(args, error);
            return;
        }

        if (args.Length <= 3)
        {
            @event.LogCommandErrorAndSkip(args, "insufficient arguments");
            return;
        }

        string[] command = args[3..];

        Point point = new(x, y);
        Dictionary<Point, List<string[]>> dictionary = _currentEventCommandPoints.Value;

        if (!dictionary.TryGetValue(point, out List<string[]>? data))
        {
            dictionary[point] = data = [];
        }

        data.Add(command);
        @event.CurrentCommand++;
    }

    internal static void Command(Event @event, string[] args, EventContext context)
    {
        if (@event.playerControlSequence)
        {
            return;
        }

        var control = _currentControlPoints.Value;
        control.Clear();
        for (int i = 1; i < args.Length; i += 2)
        {
            if (!ArgUtility.TryGetPoint(args, i, out var p, out var error))
            {
                @event.LogCommandErrorAndSkip(args, error);
                return;
            }

            control.Add(p);
        }

        if (control.Count > 0)
        {
            @event.setUpPlayerControlSequence(Key);
            @event.onEventFinished += () =>
            {
                ModEntry.ModMonitor.Log($"Event finished, cleaning up.");
                _currentActionPoints.Value.Clear();
                _currentControlPoints.Value.Clear();
                _currentEventCommandPoints.Value.Clear();
            };
            ModEntry.ModMonitor.Log($"okay, player control sequence set up with {string.Join(',', control)}");
        }
        else
        {
            @event.CurrentCommand++;
        }
    }

    internal static void RemoveActionForTile(Event @event, string[] args, EventContext context)
    {
        for (int i = 1; i < args.Length; i += 2)
        {
            if (!ArgUtility.TryGetPoint(args, i, out var p, out var error))
            {
                @event.LogCommandErrorAndSkip(args, error);
                return;
            }

            _currentActionPoints.Value.Remove(p);
            _currentEventCommandPoints.Value.Remove(p);
        }
        @event.CurrentCommand++;
    }

    internal static void RemoveActionForAllTiles(Event @event, string[] args, EventContext context)
    {
        _currentActionPoints.Value.Clear();
        _currentControlPoints.Value.Clear();
        @event.CurrentCommand++;
    }

    internal static void Apply(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(Event), nameof(Event.receiveActionPress)),
            prefix: new (typeof(PlayerControl), nameof(PrefixEventClick)));

        harmony.Patch(
            original: AccessTools.Method(typeof(Game1), nameof(Game1.eventFinished)),
            prefix: new(typeof(PlayerControl), nameof(PrefixEndEvent)));
    }

    private static bool PrefixEventClick(Event __instance, int xTile, int yTile)
    {
        if (__instance.playerControlSequenceID != Key)
        {
            return true;
        }

        try
        {
            if (_currentControlPoints.Value.Count == 0)
            {
                // no more target points, how the hell did that happen.
                ModEntry.ModMonitor.Log($"Missing target point for atravita.PlayerControl for event {__instance.id}.", LogLevel.Warn);
                __instance.CurrentCommand++;
                __instance.EndPlayerControlSequence();
                return false;
            }

            var target = new Point(xTile, yTile);
            if (_currentControlPoints.Value.Remove(target))
            {
                Game1.playSound("coin");
            }

            if (_currentActionPoints.Value.TryGetValue(target, out var actions))
            {
                foreach (var @action in actions)
                {
                    if (!TriggerActionManager.TryRunAction(@action, out var error, out var exception))
                    {
                        if (exception is not null)
                        {
                            ModEntry.ModMonitor.LogError($"applying action {@action} from eventId {__instance.id} - {error}", exception);
                        }
                        else
                        {
                            ModEntry.ModMonitor.Log($"Error applying action {@action} - {error} from eventId {__instance.id}.", LogLevel.Warn);
                        }
                    }
                }

                _currentActionPoints.Value.Remove(target);
            }

            if (_currentEventCommandPoints.Value.TryGetValue(target, out var cmds))
            {
                int current_command = __instance.CurrentCommand;
                foreach (var cmd in cmds)
                {
                    try
                    {
                        __instance.tryEventCommand(Game1.currentLocation, Game1.currentGameTime, cmd);
                    }
                    finally
                    {
                        __instance.CurrentCommand = current_command;
                    }
                }

                _currentEventCommandPoints.Value.Remove(target);
            }

            if (_currentControlPoints.Value.Count == 0)
            {
                ModEntry.ModMonitor.Log("End of playercontrol, returning to normal event.");
                __instance.CurrentCommand++;
                __instance.EndPlayerControlSequence();
            }

            return false;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("overriding playercontrol", ex);
            __instance.CurrentCommand++;
            __instance.EndPlayerControlSequence();
            return true;
        }
    }

    private static void PrefixEndEvent()
    {
        _currentActionPoints.Value.Clear();
        _currentControlPoints.Value.Clear();
    }
}