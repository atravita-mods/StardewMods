using StardewValley.Delegates;

namespace AtraCore.Framework.EventCommands;

/// <summary>
/// Branches event if the GSQ evaluates to true.
/// </summary>
internal static class BranchIf
{
    /// <inheritdoc cref="EventCommandDelegate"/>
    internal static void BranchIfCommand(Event @event, string[] args, EventContext context)
    {
        if (!ArgUtility.TryGet(args, 1, out string? branchName, out string? error) || !ArgUtility.TryGetRemainder(args, 2, out string query, out error))
        {
            context.LogErrorAndSkip(error);
            return;
        }

        if (!GameStateQuery.CheckConditions(query, location: context.Location, random: Random.Shared))
        {
            ModEntry.ModMonitor.VerboseLog($"No branch for {@event.id} - {@event.GetCurrentCommand()}");
            @event.CurrentCommand++;
            return;
        }

        Event.DefaultCommands.SwitchEvent(@event, args, context);
    }
}
