using Microsoft.Xna.Framework;

using StardewValley.Objects;

namespace SinZsEventTester.Framework;

/// <summary>
/// A struct that wraps a simple command.
/// </summary>
/// <param name="monitor">the monitor instance to use.</param>
internal struct SimpleConsoleCommand(IMonitor monitor)
{
    /// <summary>
    /// Causes the phone to show a specific call, or the first matching unseen call if not specified.
    /// </summary>
    /// <param name="call">Call to check.</param>
    internal readonly void RingPhone(string? call = null)
    {
        if (Game1.currentLocation is not GameLocation loc)
        {
            monitor.Log($"Please load a save.", LogLevel.Error);
            return;
        }

        if (!loc.Objects.Values.Any(static item => item is Phone))
        {
            List<Vector2> open = Utility.recursiveFindOpenTiles(loc, Game1.player.Tile, 2, 100);
            if (open.Count < 2 || !loc.Objects.TryAdd(open[1], new Phone(Vector2.Zero)))
            {
                monitor.Log($"Could not find empty spot to place phone.");
            }
        }
        if (call is null)
        {
            DeterministicRandom notActuallyRandom = new();
            call = Phone.PhoneHandlers.Select(handler => handler.CheckForIncomingCall(notActuallyRandom)).FirstOrDefault();
        }
        if (call is not null)
        {
            monitor.Log($"Ringing phone for {call}.", LogLevel.Info);
            Phone.intervalsToRing = 3;
            Game1.player.team.ringPhoneEvent.Fire(call);
        }
        else
        {
            monitor.Log($"Could not find valid phone call?", LogLevel.Error);
        }
    }

    internal readonly void GetTrack()
    {
        if (Game1.currentSong is { } song)
        {
            if (song is DummyCue)
            {
                monitor.Log($"Track is a dummy song, audio not enabled.", LogLevel.Warn);
                return;
            }

            monitor.Log($"Track is {song.Name}. Playing: {song.IsPlaying}.");
        }
    }

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

    internal readonly void RandomizeFriendships()
    {
        Utility.ForEachVillager(static villager =>
        {
            if (!villager.CanSocialize)
            {
                return true;
            }
            if (!Game1.player.friendshipData.TryGetValue(villager.Name, out var friendship))
            {
                Game1.player.friendshipData[villager.Name] = friendship = new Friendship();
            }

            friendship.Points = Random.Shared.Next(Utility.GetMaximumHeartsForCharacter(villager) * 250);
            return true;
        });
    }
}

/// <summary>
/// A RNG that always returns 0.
/// </summary>
file sealed class DeterministicRandom : Random
{
    /// <summary>
    /// Always returns 0.
    /// </summary>
    /// <returns>Returns 0.</returns>
    public override double NextDouble() => 0;
}