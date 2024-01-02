using AtraShared.ConstantsAndEnums;

namespace LastDayToPlantRedux;

/// <summary>
/// The API for this mod.
/// </summary>
public interface ILastDayToPlantAPI
{
    /// <summary>
    /// Gets the days needed for a specific crop for to grow.
    /// </summary>
    /// <param name="profession">Profession.</param>
    /// <param name="fertilizer">ID of fertilizer.</param>
    /// <param name="crop">crop ID.</param>
    /// <param name="season">The season to check for.</param>
    /// <returns>number of days, or null for no entry.</returns>
    /// <remarks>This is not calculated until a Low priority DayStarted. You'll need an even lower priority.</remarks>
    public int? GetDays(Profession profession, string fertilizer, string crop, StardewSeasons season);

    /// <summary>
    /// Gets all the data associated with a specific condition.
    /// </summary>
    /// <param name="profession">Profession to check.</param>
    /// <param name="fertilizer">Fertilizer to check.</param>
    /// <param name="season">The season to get.</param>
    /// <returns>The available data.</returns>
    /// <remarks>Note that profession data is not calculated if there's no player with that profession, and fertilizer data is dependent on player config.
    /// No data = not calculated.</remarks>
    public IReadOnlyDictionary<string, int>? GetAll(Profession profession, string fertilizer, StardewSeasons season);

    /// <summary>
    /// Gets the grow conditions for a specific crop.
    /// </summary>
    /// <param name="crop">Crop to check.</param>
    /// <param name="season">The season to check for.</param>
    /// <returns>(profession, fertilizer) => days.</returns>
    public KeyValuePair<KeyValuePair<Profession, string?>, int>[]? GetConditionsPerCrop(string crop, StardewSeasons season);

    /// <summary>
    /// Get the crops we have tracked.
    /// </summary>
    /// <returns>int array of tracked crops.</returns>
    public string[]? GetTrackedCrops();
}

/// <summary>
/// A enum corresponding to the profession to check.
/// </summary>
public enum Profession
{
    /// <summary>
    /// No special growing profession.
    /// </summary>
    None,

    /// <summary>
    /// Agricultralist.
    /// </summary>
    Agriculturalist,

    /// <summary>
    /// Prestiged agricultralist from WoL/MARGO.
    /// </summary>
    Prestiged,
}

// also need to copy StardewSeason.cs: https://github.com/atravita-mods/StardewMods/blob/main/AtraShared/ConstantsAndEnums/Seasons.cs . Note while that is a
// bitflag enum, this API only expects to be called with a single season at a time and will throw an error otherwise.