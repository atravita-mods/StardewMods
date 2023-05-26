using AtraBase.Toolkit.Extensions;

using AtraCore.HarmonyPatches;

using AtraShared.Utils;

namespace AtraCore.Framework.ConsoleCommands;

/// <summary>
/// Handles commands for this mod.
/// </summary>
internal static class CommandManager
{
    private const string Prefix = "av.ac.";

    internal static void Register(ICommandHelper helper)
    {
        helper.Add(
            name: $"{Prefix}.add_quest",
            documentation: "adds a quest to be tracked.",
            callback: AddQuest);
    }

    private static void AddQuest(string command, string[] args)
    {
        if (args.Length < 2)
        {
            ModEntry.ModMonitor.Log("Expected at least two values, the player to add to (use current for the current player) and the quest to add.", LogLevel.Warn);
            return;
        }

        Farmer? player;
        if (args[0].Equals("Current", StringComparison.OrdinalIgnoreCase))
        {
            player = Game1.player;
        }
        else if (long.TryParse(args[0], out long id))
        {
            player = FarmerHelpers.GetFarmerById(id);
        }
        else
        {
            player = FarmerHelpers.GetFarmerByName(args[0]);
        }

        if (player is null)
        {
            ModEntry.ModMonitor.Log($"Could not parse {args[0]} as a player. Use 'current' to refer to the current player, or use the farmer's name or UniqueID", LogLevel.Warn);
            return;
        }

        Dictionary<int, string> quests = Game1.temporaryContent.Load<Dictionary<int, string>>("Data/Quests");
        ModEntry.ModMonitor.Log($"Tracking quests for player {player.Name}", LogLevel.Info);

        for (int i = 1; i < args.Length; i++)
        {
            if (!int.TryParse(args[i], out var id))
            {
                ModEntry.ModMonitor.Log($"Could not parse {args[i]} as an integer.", LogLevel.Warn);
                continue;
            }
            if (!quests.TryGetValue(id, out var quest))
            {
                ModEntry.ModMonitor.Log($"{id} is not a valid quest.", LogLevel.Warn);
                continue;
            }

            ModEntry.ModMonitor.Log($"    Tracking quest {id} ({quest.GetNthChunk('/', 1).ToString()}).", LogLevel.Info);
            QuestTracker.TrackQuest(player, id);
        }
    }
}
