using AtraShared.ConstantsAndEnums;
using StardewValley.TerrainFeatures;

namespace LastDayToPlantRedux.Framework;
internal static class CropAndFertilizerManager
{
    private static List<HoeDirt> dirts = new();

    private static List<CropEntry> crops = new();

    private static List<int> fertilizers = new();

    private record CropEntry(int Id, StardewSeasons seasons, string growthData);

    internal static void LoadData(IGameContentHelper helper)
    {
        var data = Game1.objectInformation;

        foreach (var (index, vals) in data)
        {

        }

    }
}
