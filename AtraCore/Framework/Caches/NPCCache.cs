using CommunityToolkit.Diagnostics;

namespace AtraCore.Framework.Caches;

/// <summary>
/// A smol cache for NPCs.
/// </summary>
public static class NPCCache
{
    private static readonly Dictionary<string, WeakReference<NPC>> cache = new();

    /// <summary>
    /// Tries to find a NPC from the game.
    /// Uses cache if possible.
    /// </summary>
    /// <param name="name">Name of the NPC.</param>
    /// <returns>NPC if found, null otherwise.</returns>
    public static NPC? GetByVillagerName(string name)
    {
        Guard.IsNotNullOrWhiteSpace(name);

        if (cache.TryGetValue(name, out WeakReference<NPC>? val))
        {
            if (val.TryGetTarget(out NPC? target))
            {
                return target;
            }
            else
            {
                cache.Remove(name);
            }
        }

        NPC? npc = Game1.getCharacterFromName(name, mustBeVillager: true, useLocationsListOnly: false);
        if (npc is not null)
        {
            cache[name] = new(npc);
        }
        return npc;
    }

    internal static void Reset() => cache.Clear();
}
