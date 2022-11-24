using AtraShared.Caching;
using AtraShared.Wrappers;

using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace MoreFertilizers;

/// <summary>
/// Generalized utilities for this mod.
/// </summary>
internal static class MFUtilities
{
    private static readonly TickCache<bool> isQiQuestActive = new(() => Game1.player.team.SpecialOrderRuleActive("QI_BEANS"));

    /// <summary>
    /// Gets a value indicating whether or not the qi bean quest is active.
    /// </summary>
    internal static bool IsQiQuestActive => isQiQuestActive.GetValue();

    /// <summary>
    /// Gets a random fertilizer taking into account the player's level.
    /// </summary>
    /// <param name="level">Int skill level.</param>
    /// <returns>Fertilizer ID (-1 if not found).</returns>
    internal static int GetRandomFertilizerFromLevel(this int level)
        => Game1.random.Next(Math.Clamp((int)(level * 1.5) + 1, 0, 16)) switch
            {
                0 => ModEntry.LuckyFertilizerID,
                1 => ModEntry.JojaFertilizerID,
                2 => ModEntry.PaddyCropFertilizerID,
                3 => ModEntry.OrganicFertilizerID,
                4 => ModEntry.FruitTreeFertilizerID,
                5 => ModEntry.SeedyFertilizerID,
                6 => ModEntry.FishFoodID,
                7 => ModEntry.RadioactiveFertilizerID,
                8 => ModEntry.DeluxeFishFoodID,
                9 => ModEntry.DomesticatedFishFoodID,
                10 => ModEntry.DeluxeJojaFertilizerID,
                11 => ModEntry.DeluxeFruitTreeFertilizerID,
                12 => ModEntry.TreeTapperFertilizerID,
                13 => ModEntry.EverlastingFertilizerID,
                14 => ModEntry.MiraculousBeveragesID,
                15 => ModEntry.BountifulBushID,
                _ => ModEntry.BountifulFertilizerID,
            };

    /// <summary>
    /// Whether HoeDirt contains a crop should be considered a Joja crop for the Joja and Organic fertilizers.
    /// </summary>
    /// <param name="dirt">HoeDirt.</param>
    /// <returns>True if the HoeDirt has a joja crop.</returns>
    internal static bool HasJojaCrop(this HoeDirt dirt)
        => dirt.crop is not null && dirt.crop.IsJojaCrop();

    /// <summary>
    /// Whether the crop should be considered a Joja crop for the Joja and Organic fertilizers.
    /// </summary>
    /// <param name="crop">crop.</param>
    /// <returns>True if crop is a joja crop.</returns>
    internal static bool IsJojaCrop(this Crop crop)
    {
        string data = Game1Wrappers.ObjectInfo[crop.indexOfHarvest.Value];
        int index = data.IndexOf('/');
        if (index >= 0)
        {
            ReadOnlySpan<char> span = data.AsSpan(0, index);
            return span.Contains("Joja", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    /// <summary>
    /// Fixes IDs for all HoeDirt in a specific location.
    /// Given the idMapping.
    /// </summary>
    /// <param name="loc">Location to fix.</param>
    /// <param name="idMapping">IDMapping to use.</param>
    internal static void FixHoeDirtInLocation(this GameLocation loc, Dictionary<int, int> idMapping)
    {
        foreach (TerrainFeature terrain in loc.terrainFeatures.Values)
        {
            if (terrain is HoeDirt dirt && dirt.fertilizer.Value != 0)
            {
                if (idMapping.TryGetValue(dirt.fertilizer.Value, out int newval))
                {
                    dirt.fertilizer.Value = newval;
                }
            }
        }
        foreach (SObject obj in loc.Objects.Values)
        {
            if (obj is IndoorPot pot && pot.hoeDirt?.Value?.fertilizer?.Value is int value && value != 0)
            {
                if (idMapping.TryGetValue(value, out int newvalue))
                {
                    pot.hoeDirt.Value.fertilizer.Value = newvalue;
                }
            }
        }
    }
}