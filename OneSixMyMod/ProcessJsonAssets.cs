using Newtonsoft.Json;

using OneSixMyMod.Models.JsonAssets;

namespace OneSixMyMod;

internal static class ProcessJsonAssets
{
    internal static async Task ProcessJAMod(DirectoryInfo mods, DirectoryInfo unpacked, DirectoryInfo JAMod, string uniqueID, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        // Load crops
        var crops = new DirectoryInfo(Path.Combine(JAMod.FullName, "Crops"));
        if (crops.Exists)
        {
            foreach (var crop in crops.EnumerateDirectories())
            {
                if (crop.Name.StartsWith('.') || !crop.TryGetSingleFile("crop.json", out var file))
                {
                    continue;
                }

                CropModel? cropModel = await file.ReadJsonFile<CropModel>(token);
                if (cropModel is null)
                {
                    continue;
                }

                Console.WriteLine(cropModel);
            }
        }

        // Load shirts
        var shirts = new DirectoryInfo(Path.Combine(JAMod.FullName, "Shirts"));
        if (shirts.Exists)
        {
            foreach (var shirt in shirts.EnumerateDirectories())
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

                // load shirt textures.
            }
        }
    }
}