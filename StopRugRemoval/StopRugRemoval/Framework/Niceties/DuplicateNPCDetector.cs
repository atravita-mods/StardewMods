using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.StringHandler;

using AtraCore.Framework.Caches;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

using StardewValley;

namespace StopRugRemoval.Framework.Niceties;

/// <summary>
/// Detects and tries to fix up duplicate NPCs.
/// </summary>
internal static class DuplicateNPCDetector
{


    /// <inheritdoc cref="IGameLoopEvents.DayEnding"/>
    internal static void DayEnd()
    {
        if (Context.IsMainPlayer)
        {
            DetectDuplicateNPCs();
        }
    }

    /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
    internal static void DayStart()
    {
        if (!Context.IsMainPlayer)
        {
            return;
        }

        Dictionary<string, string> characters = Game1.content.Load<Dictionary<string, string>>("Data\\NPCDispositions");
        bool leoMoved = Game1.MasterPlayer.mailReceived.Contains("LeoMoved");
        HashSet<string> found = new(characters.Count);
        NPC? leo = null;

        Utility.ForEachVillager((npc) =>
        {
            found.Add(npc.Name);
            if (leoMoved && npc.Name == "Leo" && npc.DefaultMap != "LeoTreeHouse")
            {
                leo = npc;
            }
            return true;
        });

        if (leo is not null)
        {
            ModEntry.ModMonitor.Log("Fixing Leo's move.", LogLevel.Info);

            if (Game1.getLocationFromName("LeoTreeHouse") is not GameLocation leoTreeHouse)
            {
                ModEntry.ModMonitor.Log($"Attempted to fix up Leo's location, cannot find his treehouse.", LogLevel.Warn);
            }
            else
            {
                try
                {
                    // derived from the OnRequestLeoMoveEvent.
                    leo.DefaultMap = "LeoTreeHouse";
                    leo.DefaultPosition = new Vector2(5f, 4f) * 64f;
                    leo.faceDirection(2);
                    leo.InvalidateMasterSchedule();
                    if (leo.Schedule is not null)
                    {
                        leo.ClearSchedule();
                    }
                    leo.controller = null;
                    leo.temporaryController = null;
                    Game1.warpCharacter(leo, leoTreeHouse, new Vector2(5f, 4f));
                    leo.Halt();
                    leo.ignoreScheduleToday = false;

                    // fix up his schedule too.
                    leo.TryLoadSchedule();
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor.Log($"Failed while trying to fix Leo's move: \n\n{ex}");
                }
            }
        }

        foreach ((string name, string dispo) in characters)
        {
            if (found.Contains(name) || (Game1.year <= 1 && name == "Kent") || (name == "Leo" && !Game1.MasterPlayer.hasOrWillReceiveMail("addedParrotBoy")))
            {
                continue;
            }
            try
            {
                StreamSplit defaultpos = dispo.GetNthChunk('/', 10)
                                              .StreamSplit(null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (!defaultpos.MoveNext())
                {
                    ModEntry.ModMonitor.Log($"Badly formatted dispo for npc {name} - {dispo}", LogLevel.Warn);
                    continue;
                }

                string mapstring;
                if (name == "Leo" && leoMoved)
                {
                    mapstring = "LeoTreeHouse";
                }
                else
                {
                    mapstring = defaultpos.Current.ToString();
                }

                if (Game1.getLocationFromName(mapstring) is not GameLocation map)
                {
                    ModEntry.ModMonitor.Log($"{name} has a dispo entry for map {mapstring} which could not be found.", LogLevel.Warn);
                    continue;
                }

                int x, y;
                if (name == "Leo" && leoMoved)
                {
                    x = 5;
                    y = 4;
                }
                else
                {
                    if (!defaultpos.MoveNext() || !int.TryParse(defaultpos.Current, out x))
                    {
                        ModEntry.ModMonitor.Log($"Badly formatted dispo for npc {name}  - {dispo}", LogLevel.Warn);
                        continue;
                    }

                    if (!defaultpos.MoveNext() || !int.TryParse(defaultpos.Current, out y))
                    {
                        ModEntry.ModMonitor.Log($"Badly formatted dispo for npc {name}  - {dispo}", LogLevel.Warn);
                        continue;
                    }
                }

                ModEntry.ModMonitor.Log($"Found missing NPC {name}, adding");

                NPC npc = new(
                    sprite: new AnimatedSprite(@"Characters\" + NPC.getTextureNameForCharacter(name), 0, 16, 32),
                    position: new Vector2(x, y) * 64f,
                    defaultMap: mapstring,
                    facingDir: 0,
                    name: name,
                    schedule: null,
                    portrait: Game1.content.Load<Texture2D>(@"Portraits\" + NPC.getTextureNameForCharacter(name)),
                    eventActor: false);
                map.addCharacter(npc);
                try
                {
                    npc.TryLoadSchedule();
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor.Log($"Failed to restore schedule for missing NPC {name}\n\n{ex}", LogLevel.Warn);
                }

                // TODO: may need to fix up their dialogue as well?
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed to add missing npc {name}\n\n{ex}", LogLevel.Warn);
            }
        }

        DetectDuplicateNPCs();
    }

    private static Dictionary<string, NPC> DetectDuplicateNPCs()
    {
        Dictionary<string, NPC> found = new();
        foreach (GameLocation loc in Game1.locations)
        {
            for (int i = loc.characters.Count - 1; i >= 0; i--)
            {
                NPC? character = loc.characters[i];

                // no clue who, but someone's managing to stick nulls into the characters list.
                if (character is null)
                {
                    loc.characters.RemoveAt(i);
                    continue;
                }

                if (!character.isVillager() || character.GetType() != typeof(NPC))
                {
                    continue;
                }

                // let's populate AtraCore's cache while we're at it.
                _ = NPCCache.TryInsert(character);

                if (!found.TryAdd(character.Name, character) && character.Name != "Mister Qi")
                {
                    ModEntry.ModMonitor.Log($"Found duplicate NPC {character.Name}", LogLevel.Info);
                    if (ReferenceEquals(character, found[character.Name]))
                    {
                        ModEntry.ModMonitor.Log("    These appear to be the same instance.", LogLevel.Info);
                    }
                    if (character.id != found[character.Name].id)
                    {
                        ModEntry.ModMonitor.Log("    These appear to have different internal IDs", LogLevel.Warn);
                    }

                    if (ModEntry.Config.RemoveDuplicateNPCs)
                    {
                        loc.characters.RemoveAt(i);
                        ModEntry.ModMonitor.Log("    Removing duplicate.", LogLevel.Info);
                    }
                }
            }
        }

        return found;
    }
}
