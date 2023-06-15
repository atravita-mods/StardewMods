using System.Collections.Concurrent;

using Newtonsoft.Json;

using OneSixMyMod.Models;

namespace OneSixMyMod;

/// <summary>
/// The entry point.
/// </summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        (DirectoryInfo unpacked, DirectoryInfo mods)? dirs = FileHelpers.GetStardewPath(args.Length == 0 ? null : args[0]);

        if (dirs is null)
        {
            return;
        }

        Console.WriteLine("Reading in mod data");
        ConcurrentDictionary<string, ConcurrentQueue<(string, DirectoryInfo)>> queues = new (StringComparer.OrdinalIgnoreCase);
        await ProcessModsDir(dirs.Value.mods, queues);

        if (queues.TryGetValue("spacechase0.JsonAssets", out ConcurrentQueue<(string, DirectoryInfo)>? queue))
        {
            Console.WriteLine($"Reading in data from {queue.Count} Json Assets Mods");
            await Parallel.ForEachAsync(queue, async (val, token) => await ProcessJsonAssets.ProcessJAMod(dirs.Value.mods, dirs.Value.unpacked, val.Item2, val.Item1, token));
        }
    }

    private static async Task ProcessModsDir(DirectoryInfo mods, ConcurrentDictionary<string, ConcurrentQueue<(string, DirectoryInfo)>> queues)
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
                if (!queues.TryGetValue(model.ContentPackFor.UniqueID, out var queue))
                {
                    queues[model.ContentPackFor.UniqueID] = queue = new();
                }
                queue.Enqueue((model.UniqueID, dir));
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