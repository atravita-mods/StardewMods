using AtraShared.Utils.Extensions;

namespace CatGiftsRedux.Framework;

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

        var stock = Utility.getHatStock().Keys.ToList();
        return stock[random.Next(stock.Count)] as SObject;
    }
}
