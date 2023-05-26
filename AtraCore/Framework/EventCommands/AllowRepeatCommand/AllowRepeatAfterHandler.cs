using AtraBase.Toolkit;

using CommunityToolkit.Diagnostics;

using StardewModdingAPI.Utilities;

namespace AtraCore.Framework.EventCommands.AllowRepeatCommand;

/// <summary>
/// Handles repeating events.
/// </summary>
internal static class AllowRepeatAfterHandler
{
    /// <summary>
    /// A mapping of events to forget and the in-game day to forget them on.
    /// </summary>
    private static PerScreen<Dictionary<int, HashSet<int>>> eventsToRepeat = new(() => new());

    /// <summary>
    /// Empties and resets the events to repeat file.
    /// </summary>
    internal static void Reset()
    {
        foreach ((int _, Dictionary<int, HashSet<int>> value) in eventsToRepeat.GetActiveValues())
        {
            value.Clear();
        }
    }

    /// <summary>
    /// Loads the events to repeat file.
    /// </summary>
    /// <param name="helper">Data helper.</param>
    internal static void Load(IDataHelper helper)
    {
        Guard.IsNotNull(Constants.SaveFolderName); // should not be null if a save is loaded but might be if the player does some save edits.
        eventsToRepeat.Value = helper.ReadGlobalData<Dictionary<int, HashSet<int>>>($"{Constants.SaveFolderName.GetStableHashCode()}_{Game1.player.UniqueMultiplayerID}")
            ?? new();
    }

    /// <summary>
    /// Saves the events to repeat file.
    /// </summary>
    /// <param name="helper">Data helper.</param>
    internal static void Save(IDataHelper helper)
    {
        Guard.IsNotNull(Constants.SaveFolderName);
        Task.Run(() => helper.WriteGlobalData($"{Constants.SaveFolderName.GetStableHashCode()}_{Game1.player.UniqueMultiplayerID}", eventsToRepeat.Value))
            .ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    ModEntry.ModMonitor.Log("Wrote events file!");
                }
                else
                {
                    ModEntry.ModMonitor.Log($"Failed writing events file: {task.Status}", LogLevel.Error);
                    if (task.Exception is not null)
                    {
                        ModEntry.ModMonitor.Log(task.Exception.ToString());
                    }
                }
            });
    }

    internal static void DayEnd()
    {
        SDate now = SDate.Now();
        int days = now.DaysSinceStart;
        if (eventsToRepeat.Value.TryGetValue(days, out HashSet<int>? eventsToForget))
        {
            ModEntry.ModMonitor.Log($"Forgetting events for {now}: {string.Join(',', eventsToForget.Select(evt => evt.ToString()))}");

            int count = 0;

            for (int i = Game1.player.eventsSeen.Count - 1; i >= 0; i--)
            {
                if (eventsToForget.Contains(Game1.player.eventsSeen[i]))
                {
                    Game1.player.eventsSeen.RemoveAt(i);
                    count++;
                }
            }

            ModEntry.ModMonitor.Log($"{count} events forgotten");

            eventsToRepeat.Value.Remove(days);
        }
    }

    internal static void Add(int id, int days)
    {
        days += SDate.Now().DaysSinceStart;
        if (!eventsToRepeat.Value.TryGetValue(days, out HashSet<int>? events))
        {
            eventsToRepeat.Value[days] = events = new();
        }

        events.Add(id);
    }
}
