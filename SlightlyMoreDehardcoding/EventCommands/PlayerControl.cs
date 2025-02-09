using HarmonyLib;

using Microsoft.Xna.Framework;

using MiniAtraShared.Extensions;

using StardewModdingAPI.Utilities;

namespace SlightlyMoreDehardcoding.EventCommands;

internal static class PlayerControl
{
    private const string Key = "atravita.PlayerControl";

    private static PerScreen<HashSet<Point>> _currentControlPoints = new(() => []);

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
            if (!ArgUtility.TryGetInt(args, i, out var x, out string error)
                || !ArgUtility.TryGetInt(args, i + 1, out var y, out error))
            {
                context.LogErrorAndSkip(error);
                return;
            }

            control.Add(new Point(x, y));
        }

        if (control.Count > 0)
        {
            @event.setUpPlayerControlSequence(Key);
            ModEntry.ModMonitor.Log($"okay, player control sequence set up with {string.Join(',', control)}");
        }
        else
        {
            @event.CurrentCommand++;
        }
    }

    internal static void Apply(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(Event), nameof(Event.receiveActionPress)),
            prefix: new (typeof(PlayerControl), nameof(PrefixEventClick)));
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

}