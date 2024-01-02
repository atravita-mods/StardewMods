namespace OneSixMyMod;

using System.Collections.Concurrent;

using OneSixMyMod.Models;

/// <summary>
/// The entry point.
/// </summary>
public static class Program
{
    private static readonly Dictionary<string, Version> SupportedMods = new (StringComparer.OrdinalIgnoreCase)
    {
        ["Pathoschild.ContentPatcher"] = new Version(2, 0),
        ["Cherry.ShopTileFramework"] = new Version(0, 0),
        ["Digus.ProducerFrameworkMod"] = new Version(0, 0),
        ["spacechase0.JsonAssets"] = new Version(0, 0),
        ["Paritee.BetterFarmAnimalVariety"] = new Version(0, 0),
    };

    public static async Task Main(string[] args)
    {
        (DirectoryInfo unpacked, DirectoryInfo mods)? dirs = FileHelpers.GetStardewPath(args.Length == 0 ? null : args[0]);

        if (dirs is null)
        {
            return;
        }

        Console.WriteLine("Reading in mod data");
        ConcurrentDictionary<string, ConcurrentQueue<Manifest>> queues = new (StringComparer.OrdinalIgnoreCase);
        await ProcessModsDir(dirs.Value.mods, queues);

        if (queues.TryGetValue("spacechase0.JsonAssets", out ConcurrentQueue<Manifest>? queue))
        {
            Console.WriteLine($"Reading in data from {queue.Count} Json Assets Mods");
            await Parallel.ForEachAsync(
                source: queue,
                body: async (val, token) => await ProcessJsonAssets.ProcessJAMod(dirs.Value.mods, dirs.Value.unpacked, val, token));
        }
    }

    private static async Task ProcessModsDir(DirectoryInfo mods, ConcurrentDictionary<string, ConcurrentQueue<Manifest>> queues)
    {
        await Parallel.ForEachAsync(mods.GetDirectories(), async (dir, token) =>
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            // Console.WriteLine($"Checking {dir.FullName}");
            FileInfo[] fileInfos = dir.GetFiles("manifest.json");

            // no manifest found, search deeper.
            if (fileInfos.Length is 0)
            {
                await ProcessModsDir(dir, queues);
                return;
            }

            try
            {
                Manifest? model = await fileInfos[0].ReadJsonFile<Manifest>(token);

                if (model?.ContentPackFor is null)
                {
                    return;
                }

                if (!SupportedMods.ContainsKey(model.ContentPackFor.UniqueID))
                {
                    Console.WriteLine($"{model.UniqueID} is unsupported, you will need to convert manually.");
                    return;
                }

                if (!queues.TryGetValue(model.ContentPackFor.UniqueID, out ConcurrentQueue<Manifest>? queue))
                {
                    queues[model.ContentPackFor.UniqueID] = queue = new ();
                }

                model.Location = dir;
                queue.Enqueue(model);
                Console.WriteLine("Found content pack, queuing " + dir.FullName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Issue while opening {dir.FullName}, skipping.");
                Console.WriteLine(ex.ToString());
            }
        });
    }
}