using AtraShared.Utils.Extensions;

namespace CatGiftsRedux.Framework.Pickers;

/// <summary>
/// Picks a random unlocked hat.
/// </summary>
internal static class HatPicker
{
    /// <summary>
    /// Picks a random unlocked hat.
    /// </summary>
    /// <param name="random">Random instance.</param>
    /// <returns>Hat.</returns>
    internal static SObject? Pick(Random random)
    {
        ModEntry.ModMonitor.DebugOnlyLog("Picked hats");

        Dictionary<ISalable, int[]>? stock = Utility.getHatStock();
        return stock.Count != 0 ? stock.ElementAt(random.Next(stock.Count)).Key as SObject : null;
    }
}
