using CommunityToolkit.Diagnostics;

using StardewValley.Locations;

namespace AtraCore.Framework.Caches;

/// <summary>
/// A smol cache for NPCs.
/// </summary>
public static class NPCCache
{
    private static readonly Dictionary<string, WeakReference<NPC>> cache = new();

    public static bool TryInsert(NPC npc)
    {
        Guard.IsNotNull(npc);
        if (!npc.isVillager() || string.IsNullOrWhiteSpace(npc.Name) | npc.GetType() != typeof(NPC))
        {
            return false;
        }
        return cache.TryAdd(npc.Name, new WeakReference<NPC>(npc));
    }

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
        if (npc is not null && npc.GetType() == typeof(NPC))
        {
            cache[name] = new(npc);
        }

        // check the movie theater as well. These **might** be duplicates
        // so we'll leave you guys uncached for now.
        if (npc is null && Game1.getLocationFromName("MovieTheater") is MovieTheater theater)
        {
            ModEntry.ModMonitor.Log($"Searching movie theater for npc {name}");
            foreach (var character in theater.characters)
            {
                if (character.isVillager() && character.Name == name)
                {
                    npc = character;
                }
            }
        }

        return npc;
    }

    internal static void Reset() => cache.Clear();
}
