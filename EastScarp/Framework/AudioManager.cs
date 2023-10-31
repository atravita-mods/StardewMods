namespace EastScarp.Framework;

using EastScarp.Models;

using StardewModdingAPI.Utilities;

/// <summary>
/// Manages playing audio cues.
/// </summary>
internal static class AudioManager
{
    private static PerScreen<WeakReference<ICue>?> _current = new();

    private static void PlaySound(List<AmbientSound> sound, SpawnTrigger trigger, GameLocation location, Farmer farmer)
    {
        // hey, I still have a cue going, let's not.
        if (_current.Value?.TryGetTarget(out var cue) == true && !cue.IsStopped)
        {
            return;
        }
        else
        {
            _current.Value = null;
        }
    }
}
