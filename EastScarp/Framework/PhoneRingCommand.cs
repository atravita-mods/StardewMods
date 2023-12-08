using Microsoft.Xna.Framework;

using StardewValley.Objects;

namespace EastScarp.Framework;
internal static class PhoneRingCommand
{
    internal static void RingPhone(string command, string[] args)
    {
        if (Game1.currentLocation is not GameLocation loc)
        {
            ModEntry.ModMonitor.Log($"Please load a save.", LogLevel.Error);
            return;
        }

        if (!loc.Objects.Values.Any(static item => item is Phone))
        {
            List<Vector2> open = Utility.recursiveFindOpenTiles(loc, Game1.player.Tile, 2, 100);
            if (open.Count < 2 || !loc.Objects.TryAdd(open[1], new Phone(Vector2.Zero)))
            {
                ModEntry.ModMonitor.Log($"Could not find empty spot to place phone.");
            }
        }

        var notActuallyRandom = new DeterministicRandom();
        var call = Phone.PhoneHandlers.Select(handler => handler.CheckForIncomingCall(notActuallyRandom)).FirstOrDefault();
        if (call is not null)
        {
            ModEntry.ModMonitor.Log($"Ringing phone for {call}");
            Phone.intervalsToRing = 3;
            Game1.player.team.ringPhoneEvent.Fire(call);
        }
        else
        {
            ModEntry.ModMonitor.Log($"Could not find valid phone call?", LogLevel.Error);
        }
    }
}

file sealed class DeterministicRandom: Random
{
    public override double NextDouble() => 0f;
}