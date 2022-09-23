using StardewValley.Characters;
using StardewValley.Monsters;

namespace AtraShared.Utils;

public static class NPCHelpers
{
    public static IEnumerable<NPC> GetNPCs()
    {
        foreach (var loc in Game1.locations)
        {
            foreach (var npc in loc.characters)
            {
                if (npc is not null && npc is not Horse or Junimo or Pet or Child or Monster)
                {
                    yield return npc;
                }
            }
        }
    }
}
