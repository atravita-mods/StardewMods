using System.Reflection;
using System.Text.RegularExpressions;
using Netcode;

namespace SinZsEventTester.Framework.BulkRemoveCommand;
internal struct BulkAddRemoveCommand(IMonitor monitor)
{
    private static readonly Dictionary<GameDataType, Action<Regex, Farmer>> RemoveActors = new()
    {
        [GameDataType.Mail] = (re, farmer) => farmer.mailReceived.RemoveWhere(a => re.IsMatch(a)),
        [GameDataType.Events] = (re, farmer) => farmer.eventsSeen.RemoveWhere(a => re.IsMatch(a)),
        [GameDataType.Crafting] = (re, farmer) => farmer.craftingRecipes.RemoveWhere(a => re.IsMatch(a.Key)),
        [GameDataType.Cooking] = (re, farmer) => farmer.cookingRecipes.RemoveWhere(a => re.IsMatch(a.Key)),
        [GameDataType.ConversationTopic] = (re, farmer) => farmer.activeDialogueEvents.RemoveWhere(a => re.IsMatch(a.Key)),
        [GameDataType.NetWorldState] = (re, farmer) =>
        {
            Game1.worldStateIDs.RemoveWhere(a => re.IsMatch(a));
            (Game1.netWorldState.GetType().GetField("worldStateIDs", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Game1.netWorldState) as NetStringHashSet)?.RemoveWhere(a => re.IsMatch(a));
        },
    };

    private static readonly Dictionary<GameDataType, Action<Regex, Farmer>> AddActors = new()
    {
        [GameDataType.Crafting] = (re, farmer) =>
        {
            foreach (string? k in DataLoader.CraftingRecipes(Game1.content).Keys.Where(k => re.IsMatch(k)))
            {
                farmer.craftingRecipes.TryAdd(k, 0);
            }
        },

        [GameDataType.Cooking] = (re, farmer) =>
        {
            foreach (string? k in DataLoader.CookingRecipes(Game1.content).Keys.Where(k => re.IsMatch(k)))
            {
                farmer.cookingRecipes.TryAdd(k, 0);
            }
        },

        [GameDataType.Events] = (re, farmer) =>
        {
            foreach (GameLocation? location in Game1.locations)
            {
                if (!location.TryGetLocationEvents(out _, out Dictionary<string, string>? events))
                {
                    continue;
                }

                foreach (string k in events.Keys)
                {
                    // not a real event.
                    if (!int.TryParse(k, out _) && !k.Contains('/'))
                    {
                        continue;
                    }

                    string id = k.GetNthChunk('/').ToString();

                    if (re.IsMatch(id))
                    {
                        farmer.eventsSeen.Add(id);
                    }
                }
            }
        },
    };

    internal void Add(string[] args)
    {
        if (!ArgUtility.TryGetEnum(args, 0, out GameDataType value, out string? error))
        {
            monitor.Log(error, LogLevel.Warn);
            return;
        }

        if (!AddActors.TryGetValue(value, out Action<Regex, Farmer>? actor))
        {
            monitor.Log($"Type to remove is invalid: {value}", LogLevel.Warn);
            return;
        }

        foreach (string? regex in args.AsSpan(1))
        {
            Regex re = new(regex);
            actor(re, Game1.player);
        }
    }

    internal void Remove(string[] args)
    {
        if (!ArgUtility.TryGetEnum(args, 0, out GameDataType value, out string? error))
        {
            monitor.Log(error, LogLevel.Warn);
            return;
        }

        if (!RemoveActors.TryGetValue(value, out Action<Regex, Farmer>? actor))
        {
            monitor.Log($"Type to remove is invalid: {value}", LogLevel.Warn);
            return;
        }

        foreach (string? regex in args.AsSpan(1))
        {
            Regex re = new(regex);
            actor(re, Game1.player);
        }
    }
}
