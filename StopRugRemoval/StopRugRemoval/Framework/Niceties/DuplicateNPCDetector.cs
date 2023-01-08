using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.StringHandler;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StopRugRemoval.Framework.Niceties;
internal static class DuplicateNPCDetector
{
    internal static void DayEnd()
    {
        DetectDuplicateNPCs();
    }

    internal static void DayStart()
    {
        HashSet<string> found = new();

        foreach (var character in Utility.getAllCharacters())
        {
            found.Add(character.Name);
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
                GameLocation map = Game1.getLocationFromName(mapstring);
                if (map is null)
                {
                    ModEntry.ModMonitor.Log($"{name} has a dispo entry for map {mapstring} which could not be found.", LogLevel.Warn);
                    continue;
                }

                if (!defaultpos.MoveNext() || int.TryParse(defaultpos.Current, out var x))
                {
                    ModEntry.ModMonitor.Log($"Badly formatted dispo for npc {name}");
                    continue;
                }

                if (!defaultpos.MoveNext() || int.TryParse(defaultpos.Current, out var y))
                {
                    ModEntry.ModMonitor.Log($"Badly formatted dispo for npc {name}");
                    continue;
                }

                map.addCharacter(
                    new NPC(
                        sprite: new AnimatedSprite("Characters\\" + NPC.getTextureNameForCharacter(name), 0, 16, 32),
                        position: new Vector2(x, y) * 64f,
                        defaultMap: mapstring,
                        facingDir: 0,
                        name: name,
                        schedule: null,
                        portrait: Game1.content.Load<Texture2D>("Portraits\\" + NPC.getTextureNameForCharacter(name)),
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

                if (!found.TryAdd(character.Name, character))
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
