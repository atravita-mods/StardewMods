using HarmonyLib;

namespace SlightlyMoreDehardcoding.EventCommands;
internal static class FestivalCommands
{
    #region delegates

    private static readonly Lazy<AccessTools.FieldRef<Event, NPC>> _festivalHostSetter = new(() =>
    {
        return AccessTools.FieldRefAccess<Event, NPC>("festivalHost");
    });

    private static readonly Lazy<AccessTools.FieldRef<Event, string>> _festivalHostMessageGetter = new(() =>
    {
        return AccessTools.FieldRefAccess<Event, string>("hostMessageKey");
    });

    #endregion

    internal static void SetFestivalHost(Event @event, string[] args, EventContext context)
    {
        if (!ArgUtility.TryGet(args, 1, out var host, out string? error, allowBlank: false))
        {
            context.LogErrorAndSkip(error);
            return;
        }

        if (@event.getActorByName(host) is not NPC npc)
        {
            context.LogErrorAndSkip($"could not find actor by name {host}");
            return;
        }

        _festivalHostSetter.Value(@event) = npc;

        ArgUtility.TryGetOptionalRemainder(args, 2, out string dialogue);
        if (dialogue is not null)
        {
            _festivalHostMessageGetter.Value(@event) = dialogue;
        }
    }
}
