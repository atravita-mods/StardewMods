namespace SinZsEventTester.Framework;

/// <summary>
/// A struct that wraps a simple command.
/// </summary>
/// <param name="monitor">the monitor instance to use.</param>
internal struct SimpleConsoleCommand(IMonitor monitor)
{
    /// <summary>
    /// Forgets that the following events were seen.
    /// </summary>
    /// <param name="evts">The events to forget.</param>
    internal readonly void ForgetEvents(string[] evts)
    {
        foreach (string evt in evts)
        {
            if (Game1.player.eventsSeen.Remove(evt))
            {
                monitor.Log($"Forgetting {evt} for {Game1.player.Name}", LogLevel.Debug);
            }
        }
    }

    /// <summary>
    /// Forgets that the following mail was received.
    /// </summary>
    /// <param name="mails">Mails to forget.</param>
    internal readonly void ForgetMail(string[] mails)
    {
        foreach (string mail in mails)
        {
            if (Game1.player.mailReceived.Remove(mail))
            {
                monitor.Log($"Forgetting {mail} for {Game1.player.Name}", LogLevel.Debug);
            }
        }
    }

    /// <summary>
    /// Forgets that the following triggers were run.
    /// </summary>
    /// <param name="triggers">Triggers to forget.</param>
    internal readonly void ForgetTriggers(string[] triggers)
    {
        foreach (string trigger in triggers)
        {
            if (Game1.player.triggerActionsRun.Remove(trigger))
            {
                monitor.Log($"Forgetting trigger {trigger} for {Game1.player.Name}", LogLevel.Debug);
            }
        }
    }

    /// <summary>
    /// Checks over all event preconditions in the game.
    /// </summary>
    internal readonly void CheckPreconditions()
    {
        foreach (GameLocation? location in Game1.locations)
        {
            if (!location.TryGetLocationEvents(out string? assetName, out Dictionary<string, string>? events) || events.Count == 0)
            {
                monitor.Log($"No events for {location.Name}.");
                continue;
            }

            foreach (string evt in events.Keys)
            {
                string[] splits = evt.Split('/');
                if (splits.Length < 2 || (splits.Length == 2 && string.IsNullOrWhiteSpace(splits[1])))
                {
                    monitor.Log($"'{evt}' is either fork or has no preconditions.");
                    continue;
                }

                monitor.Log($"Checking preconditions for {evt}", LogLevel.Info);
                string key = splits[0];
                foreach (string? s in splits.AsSpan(1))
                {
                    if (string.IsNullOrEmpty(s))
                    {
                        monitor.Log($"\t has empty precondition, which is not allowed.");
                        continue;
                    }
                    Event.CheckPrecondition(location, key, s);
                }
            }
        }
    }

}
