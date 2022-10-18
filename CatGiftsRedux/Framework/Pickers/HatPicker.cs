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

        List<ISalable>? stock = Utility.getHatStock().Keys.ToList();
        return stock[random.Next(stock.Count)] as SObject;
    }
}
