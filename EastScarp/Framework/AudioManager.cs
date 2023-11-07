namespace EastScarp.Framework;

using EastScarp.Models;

using StardewModdingAPI.Utilities;

using StardewValley.Extensions;

/// <summary>
/// Manages playing audio cues.
/// </summary>
internal static class AudioManager
{
    private static readonly PerScreen<WeakReference<ICue>?> _current = new();

    internal static void PlaySound(List<AmbientSound> sounds, SpawnTrigger trigger, GameLocation location, Farmer farmer)
    {
        // hey, I still have a cue going, let's not.
        if (_current.Value?.TryGetTarget(out ICue? cue) == true && cue.IsPlaying)
        {
            ModEntry.ModMonitor.VerboseLog($"Not playing audio cue as {cue.Name} is still playing.");
            return;
        }
        else
        {
            _current.Value = null;
        }

        foreach (AmbientSound sound in sounds)
        {
            if (!sound.Trigger.HasFlag(trigger))
            {
                continue;
            }

            if (!sound.Contains(farmer.TilePoint))
            {
                continue;
            }

            if (!sound.CheckCondition(location, farmer))
            {
                continue;
            }

            if (!Game1.soundBank.Exists(sound.Sound))
            {
                ModEntry.ModMonitor.LogOnce($"Cue {sound.Sound} does not seem to exist.", LogLevel.Warn);
                continue;
            }

            try
            {
                ModEntry.ModMonitor.VerboseLog($"Playing sound cue {sound.Sound}");
                int? pitch = null;
                if (sound.Pitches?.Count is > 0)
                {
                    pitch = Random.Shared.ChooseFrom(sound.Pitches);
                }

                ICue? newCue = null;
                if (pitch is not null)
                {
                    Game1.playSound(sound.Sound, pitch.Value, out newCue);
                }
                else
                {
                    Game1.playSound(sound.Sound, out newCue);
                }

                if (newCue is not null)
                {
                    _current.Value = new (newCue);
                }
                break;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed to play cue {sound.Sound}: {ex}", LogLevel.Error);
            }
        }
    }
}
