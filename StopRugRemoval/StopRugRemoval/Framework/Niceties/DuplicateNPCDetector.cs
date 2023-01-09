using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.StringHandler;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StopRugRemoval.Framework.Niceties;
internal static class DuplicateNPCDetector
{
    internal static void DayEnd()
    {
        if (!Context.IsMainPlayer)
        {
            return;
        }

        DetectDuplicateNPCs();
    }

    internal static void DayStart()
    {
        if (!Context.IsMainPlayer)
        {
            return;
        }

        HashSet<string> found = new();
        bool leoMoved = Game1.MasterPlayer.mailReceived.Contains("LeoMoved");

        foreach (NPC? character in Utility.getAllCharacters())
        {
            found.Add(character.Name);

            if (character.Name == "Leo" && leoMoved && character.DefaultMap != "LeoTreeHouse")
            {
                ModEntry.ModMonitor.Log("Fixing Leo's move.", LogLevel.Info);

                try
                {
                    // derived from the OnRequestLeoMoveEvent.
                    character.DefaultMap = "LeoTreeHouse";
                    character.DefaultPosition = new Vector2(5f, 4f) * 64f;
                    character.faceDirection(2);
                    character.InvalidateMasterSchedule();
                    if (character.Schedule is not null)
                    {
                        character.Schedule = null;
                    }
                    character.controller = null;
                    character.temporaryController = null;
                    Game1.warpCharacter(character, Game1.getLocationFromName("LeoTreeHouse"), new Vector2(5f, 4f));
                    character.Halt();
                    character.ignoreScheduleToday = false;

                    // fix up his schedule too.
                    character.Schedule = character.getSchedule(Game1.dayOfMonth);
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor.Log($"Failed while trying to fix Leo's move: \n\n{ex}");
                }
            }
        }

        var dispos = Game1.content.Load<Dictionary<string, string>>("Data\\NPCDispositions");

        foreach (var (name, dispo) in dispos)
        {
            if (found.Contains(name) || (Game1.year <= 1 && name == "Kent") || (name == "Leo" && !Game1.MasterPlayer.hasOrWillReceiveMail("addedParrotBoy")))
            {
                continue;
            }
            try
            {
                StreamSplit defaultpos = dispo.GetNthChunk('/', 10).StreamSplit(' ');
                if (!defaultpos.MoveNext())
                {
                    ModEntry.ModMonitor.Log($"Badly formatted dispo for npc {name}");
                    continue;
                }

                string mapstring = defaultpos.Current.ToString();

                if (name == "Leo" && leoMoved)
                {
                    mapstring = "LeoTreeHouse";
                }

                GameLocation map = Game1.getLocationFromName(mapstring);
                if (map is null)
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
                    if (!defaultpos.MoveNext() || int.TryParse(defaultpos.Current, out x))
                    {
                        ModEntry.ModMonitor.Log($"Badly formatted dispo for npc {name}");
                        continue;
                    }

                    if (!defaultpos.MoveNext() || int.TryParse(defaultpos.Current, out y))
                    {
                        ModEntry.ModMonitor.Log($"Badly formatted dispo for npc {name}");
                        continue;
                    }
                }

                ModEntry.ModMonitor.Log($"Found missing NPC {name}, adding");

                map.addCharacter(
                    new NPC(
                        sprite: new AnimatedSprite(@"Characters\" + NPC.getTextureNameForCharacter(name), 0, 16, 32),
                        position: new Vector2(x, y) * 64f,
                        defaultMap: mapstring,
                        facingDir: 0,
                        name: name,
                        schedule: null,
                        portrait: Game1.content.Load<Texture2D>(@"Portraits\" + NPC.getTextureNameForCharacter(name)),
                        eventActor: false));
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed to add missing npc {name}\n\n{ex}");
            }
        }

        DetectDuplicateNPCs();
    }

    private static void DetectDuplicateNPCs()
    {
        Dictionary<string, NPC> found = new();
        foreach (GameLocation loc in Game1.locations)
        {
            for (int i = loc.characters.Count - 1; i >= 0; i--)
            {
                NPC character = loc.characters[i];
                if (!character.isVillager())
                {
                    continue;
                }

                if (!found.TryAdd(character.Name, character) && character.Name != "Mister Qi")
                {
                    ModEntry.ModMonitor.Log($"Found duplicate NPC {character.Name}");
                    if (object.ReferenceEquals(character, found[character.Name]))
                    {
                        ModEntry.ModMonitor.Log("These appear to be the same instance.");
                    }

                    if (ModEntry.Config.RemoveDuplicateNPCs)
                    {
                        loc.characters.RemoveAt(i);
                    }
                }
            }
        }
    }
}
