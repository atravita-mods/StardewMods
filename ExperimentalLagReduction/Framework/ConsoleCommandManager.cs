using AtraCore.Framework.Caches;
using AtraCore.Framework.ItemManagement;

using AtraShared.ConstantsAndEnums;
using AtraShared.Wrappers;

using ExperimentalLagReduction.HarmonyPatches;

namespace ExperimentalLagReduction.Framework;

/// <summary>
/// Manages console commands for this mod.
/// </summary>
internal static class ConsoleCommandManager
{
    private const string Prefix = "av.elr.";

    /// <summary>
    /// Registers the console commands for this mod.
    /// </summary>
    /// <param name="commandHelper">Console command helper.</param>
    internal static void Register(ICommandHelper commandHelper)
    {
        commandHelper.Add(
            name: $"{Prefix}dump_cache",
            documentation: "Dumps the pathfinder's cache to the terminal.",
            callback: static (_, _) => Rescheduler.PrintCache());

        commandHelper.Add(
            name: $"{Prefix}get_path_from",
            documentation: "Gets the path between two locations, using the following gender constraints.",
            callback: GetPath);

        commandHelper.Add(
            name: $"{Prefix}print_report",
            documentation: "Prints a little report for the macro scheduler.",
            callback: Report);

        commandHelper.Add(
            name: $"{Prefix}get_gift_taste",
            documentation: "Gets the gift taste for a specific item (as calculated by this mod.",
            callback: GetGiftTasteFor);
    }

    private static void GetPath(string command, string[] args)
    {
        if (args.Length < 2 || args.Length > 4)
        {
            ModEntry.ModMonitor.Log("Expected two to four arguments", LogLevel.Error);
            return;
        }

        // whether or not to search and cache.
        bool force = false;
        if (args.Length == 4)
        {
            if (!bool.TryParse(args[3], out force))
            {
                ModEntry.ModMonitor.Log($"{args[3]} could not be parsed as a bool, ignoring.", LogLevel.Warn);
            }
        }

        Gender gender = Gender.Undefined;
        if (args.Length > 2)
        {
            if (!GenderExtensions.TryParse(args[2], out gender, ignoreCase: true) || !GenderExtensions.IsDefined(gender) || gender == Gender.Invalid)
            {
                ModEntry.ModMonitor.Log($"Could not parse {args[2]} as a valid gender.", LogLevel.Error);
                gender = Gender.Undefined;
            }
        }

        if (Rescheduler.TryGetPathFromCache(args[0], args[1], (int)gender, out List<string>? path))
        {
            if (path is not null)
            {
                ModEntry.ModMonitor.Log(string.Join("->", path), LogLevel.Info);
            }
            else
            {
                ModEntry.ModMonitor.Log("That path is invalid.", LogLevel.Info);
            }
        }
        else
        {
            ModEntry.ModMonitor.Log($"That path was not cached.", LogLevel.Info);

            if (force)
            {
                GameLocation? startLocation = Game1.getLocationFromName(args[0]);
                if (startLocation is null)
                {
                    ModEntry.ModMonitor.Log($"The starting location {args[0]} does not exist.", LogLevel.Warn);
                    return;
                }

                GameLocation? endLocation = Game1.getLocationFromName(args[1]);
                if (endLocation is null)
                {
                    ModEntry.ModMonitor.Log($"The ending location {args[1]} does not exist.", LogLevel.Warn);
                    return;
                }

                List<string>? calcPath = Rescheduler.GetPathFor(startLocation, endLocation, gender, ModEntry.Config.AllowPartialPaths);

                if (calcPath is null)
                {
                    ModEntry.ModMonitor.Log($"There is no valid path between {startLocation.Name} and {endLocation.Name}", LogLevel.Info);
                }
                else
                {
                    ModEntry.ModMonitor.Log(string.Join("->", calcPath), LogLevel.Info);
                }
            }
        }
    }

    private static void Report(string command, string[] args)
    {
        ModEntry.ModMonitor.Log($"Total locations: {Game1.locations.Count}", LogLevel.Info);
        ModEntry.ModMonitor.Log($"Cached routes: {Rescheduler.CacheCount}", LogLevel.Info);

#if DEBUG
        ModEntry.ModMonitor.Log($"Timing: {string.Join(", ", Rescheduler.Watches.Select(static watch => $"{watch.ElapsedMilliseconds} ms"))}", LogLevel.Info);
        ModEntry.ModMonitor.Log($"Total: {Rescheduler.Watches.Sum(static watch => watch.ElapsedMilliseconds)} ms", LogLevel.Info);

        ModEntry.ModMonitor.Log($"Cache hit percentage: {Rescheduler.CacheHitRatio * 100}%", LogLevel.Info);
        ModEntry.ModMonitor.Log($"{Rescheduler.CacheHits} hits out of {Rescheduler.CacheCount} routes cached: {((float)Rescheduler.CacheHits / Rescheduler.CacheCount) * 100}%", LogLevel.Info);
#endif
    }

    private static void GetGiftTasteFor(string command, string[] args)
    {
        if (args.Length < 2)
        {
            ModEntry.ModMonitor.Log("Expected at least two arguments, the NPC and the item", LogLevel.Warn);
            return;
        }

        NPC? npc = NPCCache.GetByVillagerName(args[0], searchTheater: true);
        if (npc is null)
        {
            ModEntry.ModMonitor.Log($"Could not find NPC by the name {args[0]}", LogLevel.Warn);
            return;
        }

        for (int i = 1; i < args.Length; i++)
        {
            string contender = args[i];
            if (!int.TryParse(contender, out int id) || !Game1Wrappers.ObjectInfo.ContainsKey(id))
            {
                id = DataToItemMap.GetID(ItemTypeEnum.SObject, contender);
            }
            if (id < 0)
            {
                ModEntry.ModMonitor.Log($"{contender} doesn't seem to be a valid item.", LogLevel.Warn);
                continue;
            }

            SObject obj = new(id, 1);

            // fine for this to be non-exhaustive, it's only used in a console command.
            // let it throw.
            string taste = OverrideGiftTastes.GetGiftTaste(npc, obj) switch
            {
                NPC.gift_taste_hate => "hate",
                NPC.gift_taste_dislike => "dislike",
                NPC.gift_taste_neutral => "neutral",
                NPC.gift_taste_like => "like",
                NPC.gift_taste_love => "love",
            };

            ModEntry.ModMonitor.Log($"For {npc.Name}, {obj.Name} is a {taste}", LogLevel.Info);
        }
    }
}
