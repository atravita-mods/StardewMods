namespace MoreFertilizers;

/// <summary>
/// Generalized utilities for this mod.
/// </summary>
internal static class MFUtilities
{
    /// <summary>
    /// Gets a random fertilizer taking into account the player's level.
    /// </summary>
    /// <param name="level">Int skill level.</param>
    /// <returns>Fertilizer ID (-1 if not found).</returns>
    internal static int GetRandomFertilizerFromLevel(this int level)
        => Game1.random.Next(Math.Clamp(level + 1, 0, 11)) switch
            {
                0 => ModEntry.LuckyFertilizerID,
                1 => ModEntry.JojaFertilizerID,
                2 => ModEntry.PaddyCropFertilizerID,
                3 => ModEntry.OrganicFertilizerID,
                4 => ModEntry.FruitTreeFertilizerID,
                5 => ModEntry.FishFoodID,
                6 => ModEntry.DeluxeFishFoodID,
                7 => ModEntry.DomesticatedFishFoodID,
                8 => ModEntry.DeluxeJojaFertilizerID,
                9 => ModEntry.DeluxeFruitTreeFertilizerID,
                _ => ModEntry.BountifulFertilizerID,
            };
}