using AtraBase.Toolkit.Extensions;

using AtraShared.Utils.Extensions;

namespace CatGiftsRedux.Framework;

/// <summary>
/// Picks an appropriate seasonal fruit.
/// </summary>
internal static class SeasonalFruitPicker
{
    internal static SObject? Pick(Random random)
    {
        ModEntry.ModMonitor.DebugOnlyLog("Picked Seasonal Fruit");

        List<KeyValuePair<int, string>>? fruittrees = Game1.content.Load<Dictionary<int, string>>("Data/fruitTrees")
                                      .Where((kvp) => kvp.Value.GetNthChunk('/', 1).Contains(Game1.currentSeason, StringComparison.OrdinalIgnoreCase))
                                      .ToList();

        if (fruittrees.Count == 0)
        {
            return null;
        }

        KeyValuePair<int, string> fruit = fruittrees[random.Next(fruittrees.Count)];

        if (int.TryParse(fruit.Value.GetNthChunk('/', 2), out int id))
        {
            return new SObject(id, 1);
        }
        return null;
    }
}
