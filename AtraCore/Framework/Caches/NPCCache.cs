// Ignore Spelling: npc

using System.Collections.Concurrent;

using CommunityToolkit.Diagnostics;

using StardewValley.Locations;

namespace AtraCore.Framework.Caches;

#warning - todo - a way to iterate through these.

/// <summary>
/// A smol cache for NPCs.
/// </summary>
public static class NPCCache
{
    private static readonly ConcurrentDictionary<string, WeakReference<NPC>> cache = new();

    /// <summary>
    /// Checks to see if a specific name is in the cache.
    /// </summary>
    /// <param name="name">Name to check.</param>
    /// <returns>true if exists in cache, false otherwise.</returns>
    public static bool ContainsKey(string name) => cache.ContainsKey(name);

    /// <summary>
    /// Inserts a specific NPC instance into the cache.
    /// </summary>
    /// <param name="npc">NPC instance to insert.</param>
    /// <returns>True if inserted, false otherwise.</returns>
    public static bool TryInsert(NPC npc)
    {
        Guard.IsNotNull(npc);
        if (!npc.isVillager() || string.IsNullOrWhiteSpace(npc.Name) | npc.GetType() != typeof(NPC))
        {
            return false;
        }
        string name = npc.Name;
        return cache.TryAdd(string.IsInterned(name) ?? name, new WeakReference<NPC>(npc));
    }

    /// <summary>
    /// Tries to find a NPC from the game.
    /// Uses cache if possible.
    /// </summary>
    /// <param name="name">Name of the NPC.</param>
    /// <returns>NPC if found, null otherwise.</returns>
    /// <remarks>Does not search theater.</remarks>
    public static NPC? GetByVillagerName(string name) => GetByVillagerName(name, false);

    /// <summary>
    /// Tries to find a NPC from the game.
    /// Uses cache if possible.
    /// </summary>
    /// <param name="name">Name of the NPC.</param>
    /// <param name="searchTheater">Whether or not to also search the theater, which may contain NPCs who have pathed in but also can contain NPCs who are duplicates.</param>
    /// <returns>NPC if found, null otherwise.</returns>
    public static NPC? GetByVillagerName(string name, bool searchTheater)
    {
        Guard.IsNotNullOrWhiteSpace(name);

        // in multiplayer, we need to guard against the clones. So always search the current location, and replace the cache instance if needed.
        if (Context.IsMultiplayer && (!Context.IsMainPlayer || Context.IsSplitScreen)
            && Game1.currentLocation is not null)
        {
            foreach (NPC? character in Game1.currentLocation.characters)
            {
                if (!character.EventActor && character.IsVillager && character.Name == name)
                {
                    if (character.GetType() == typeof(NPC))
                    {
                        cache[string.IsInterned(name) ?? name] = new(character);
                    }
                    return character;
                }
            }
        }

        if (cache.TryGetValue(name, out WeakReference<NPC>? val))
        {
            if (val.TryGetTarget(out NPC? target))
            {
                return target;
            }
            else
            {
                cache.TryRemove(name, out _);
            }
        }

        NPC? npc = Game1.getCharacterFromName(name, mustBeVillager: true);
        if (npc is not null && npc.GetType() == typeof(NPC) && npc.currentLocation?.IsActiveLocation() == true)
        {
            cache[string.IsInterned(name) ?? name] = new(npc);
        }

        // check the movie theater as well. These **might** be duplicates
        // so we'll leave you guys uncached for now.
        if (npc is null && searchTheater && Game1.getLocationFromName("MovieTheater") is MovieTheater theater)
        {
            ModEntry.ModMonitor.Log($"Searching movie theater for npc {name}.");
            foreach (NPC? character in theater.characters)
            {
                if (!character.EventActor && character.IsVillager && character.Name == name)
                {
                    npc = character;
                    break;
                }
            }
        }

        return npc;
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    internal static void Reset() => cache.Clear();
}
