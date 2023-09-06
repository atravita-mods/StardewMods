﻿using AtraBase.Toolkit;

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
    private static readonly PerScreen<Dictionary<int, HashSet<string>>> eventsToRepeat = new(() => new());

    /// <summary>
    /// Gets the save key used to look up our saved data.
    /// </summary>
    private static string SaveKey
    {
        get
        {
            Guard.IsNotNullOrEmpty(Constants.SaveFolderName); // should not be null if a save is loaded but might be if the player does some save edits.
            return $"{Constants.SaveFolderName.GetStableHashCode()}_{Game1.player.UniqueMultiplayerID}";
        }
    }

    /// <summary>
    /// Empties and resets the events to repeat file.
    /// </summary>
    internal static void Reset()
    {
        foreach ((int _, Dictionary<int, HashSet<string>> value) in eventsToRepeat.GetActiveValues())
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
        eventsToRepeat.Value = helper.ReadGlobalData<Dictionary<int, HashSet<string>>>(SaveKey)
            ?? new();
    }

    /// <summary>
    /// Saves the events to repeat file.
    /// </summary>
    /// <param name="helper">Data helper.</param>
    internal static void Save(IDataHelper helper)
    {
        // check key on main thread in case it throws.
        string key = SaveKey;

        Task.Run(() => helper.WriteGlobalData(key, eventsToRepeat.Value.Count == 0 ? null : eventsToRepeat.Value))
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

    /// <summary>
    /// Removes the events seen if there's any tracked.
    /// </summary>
    internal static void DayEnd()
    {
        SDate now = SDate.Now();
        HashSet<string> toForget = new();
        List<int> daysHandled = new();
        foreach ((int days, HashSet<string>? eventsToForget) in eventsToRepeat.Value)
        {
            if (days > now.DaysSinceStart)
            {
                continue;
            }
            daysHandled.Add(days);
            foreach (string evt in eventsToForget)
            {
                toForget.Add(evt);
            }
        }

        if (daysHandled.Count == 0)
        {
            return;
        }

        if (toForget.Count > 0)
        {
            ModEntry.ModMonitor.Log($"For day '{now}', forgetting events {string.Join(", ", toForget)}");

            int count = 0;
            for (int i = Game1.player.eventsSeen.Count - 1; i >= 0; i--)
            {
                if (toForget.Contains(Game1.player.eventsSeen[i]))
                {
                    Game1.player.eventsSeen.RemoveAt(i);
                    count++;
                }
            }
            ModEntry.ModMonitor.Log($"{count} events forgotten.");
        }

        foreach (int day in daysHandled)
        {
            eventsToRepeat.Value.Remove(day);
        }
    }

    /// <summary>
    /// Adds an event to the list to remove.
    /// </summary>
    /// <param name="id">int id of the event.</param>
    /// <param name="days">number of days to offset by.</param>
    internal static void Add(string id, int days)
    {
        days += SDate.Now().DaysSinceStart;
        if (!eventsToRepeat.Value.TryGetValue(days, out HashSet<string>? events))
        {
            eventsToRepeat.Value[days] = events = new();
        }

        if (events.Add(id))
        {
            ModEntry.ModMonitor.Log($"Tracking event '{id}' to be forgotten on day {days}.");
        }
    }
}
