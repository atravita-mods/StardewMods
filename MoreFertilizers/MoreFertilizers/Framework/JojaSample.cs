﻿using AtraShared.Menuing;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

using StardewValley.Locations;

namespace MoreFertilizers.Framework;

#warning - remove duplicate Morris!

/// <summary>
/// Class that handles Joja giving the player a free sample of Joja fertilizer.
/// </summary>
internal static class JojaSample
{
    private static readonly PerScreen<bool> HaveRecievedSampleToday = new(() => false);

    /// <summary>
    /// Resets per-day.
    /// </summary>
    internal static void Reset()
        => HaveRecievedSampleToday.Value = false;

    /// <summary>
    /// Handles giving out the Joja sample sometimes.
    /// </summary>
    /// <param name="e">OnWarp events.</param>
    internal static void JojaSampleEvent(WarpedEventArgs e)
    {
        if (e.NewLocation is JojaMart && e.IsLocalPlayer
            && MenuingExtensions.IsNormalGameplay() && !HaveRecievedSampleToday.Value
            && !Game1.eventUp && Game1.CurrentEvent is null && Game1.random.NextDouble() <= 0.15)
        {
            string[] jojaEventstring = new[]
            {
            "continue/-100 -100/farmer 13 28 0 Morris 13 22 2/makeInvisible 21 22 1 3/ignoreCollisions farmer/",
            "ignoreCollisions Morris/skippable/viewport 13 25/move Morris 0 2 2/pause 400/",
            $"speak Morris \"{I18n.GetByKey($"joja.event.{Game1.random.Next(3)}")}\"/pause 400/end",
            };

            Event jojaEvent = new(string.Join(string.Empty, jojaEventstring))
            {
                onEventFinished = () =>
                {
                    DelayedAction.functionAfterDelay(
                        () =>
                        {
                            e.Player.addItemByMenuIfNecessaryElseHoldUp(new SObject(ModEntry.JojaFertilizerID, Game1.random.Next(2, 6)));
                        }, 100);
                },
            };
            HaveRecievedSampleToday.Value = true;
            e.NewLocation.startEvent(jojaEvent);
        }
    }
}