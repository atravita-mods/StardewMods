using System.Collections.Concurrent;

using Newtonsoft.Json;

using OneSixMyMod.Models;
using OneSixMyMod.Models.JsonAssets;

namespace OneSixMyMod;

internal static class ProcessJsonAssets
{
    internal static async Task<IEnumerable<KeyValuePair<string, string>>> ProcessJAMod(DirectoryInfo mods, DirectoryInfo unpacked, Manifest manifest, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return Enumerable.Empty<KeyValuePair<string, string>>();
        }

        if (manifest.Location is not DirectoryInfo modDir)
        {
            throw new ArgumentException($"Directory information for {manifest.UniqueID} required.");
        }

        ConcurrentDictionary<string, string> itemMap = new();

        // load objects
        DirectoryInfo objects = new DirectoryInfo(Path.Combine(modDir.FullName, "Objects"));
        if (objects.Exists)
        {
            await Parallel.ForEachAsync(
                objects.EnumerateDirectories(),
                token,
                async (obj, token) =>
                {
                    if (token.IsCancellationRequested || obj.Name.StartsWith('.') || !obj.TryGetSingleFile("object.json", out FileInfo? file))
                    {
                        return;
                    }
                });
        }

        // Load crops
        DirectoryInfo crops = new DirectoryInfo(Path.Combine(modDir.FullName, "Crops"));
        if (crops.Exists)
        {
            await Parallel.ForEachAsync(
                crops.EnumerateDirectories(),
                token,
                async (crop, token) =>
                {
                    if (token.IsCancellationRequested || crop.Name.StartsWith('.') || !crop.TryGetSingleFile("crop.json", out FileInfo? file))
                    {
                        return;
                    }

                    CropModel? cropModel = await file.ReadJsonFile<CropModel>(token);
                    if (cropModel is null)
                    {
                        return;
                    }
                });
        }

        // Load shirts
        DirectoryInfo shirts = new DirectoryInfo(Path.Combine(modDir.FullName, "Shirts"));
        if (shirts.Exists)
        {
            foreach (DirectoryInfo shirt in shirts.EnumerateDirectories())
            {
                if (shirt.Name.StartsWith('.') || !shirt.TryGetSingleFile("shirt.json", out FileInfo? file))
                {
                    continue;
                }

                ShirtModel? shirtModel = await file.ReadJsonFile<ShirtModel>(token);
                if (shirtModel is null)
                {
                    continue;
                }
            }
        }

        // Forge cannot be ported at this time.
        DirectoryInfo forge = new DirectoryInfo(Path.Combine(modDir.FullName, "Forge"));
        if (forge.Exists && forge.GetDirectories().Any())
        {
            manifest.MigrationFailureReason.Add("Forge recipes cannot be converted.");
        }

        return itemMap;
    }
}