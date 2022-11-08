namespace LastDayToPlantRedux;
public interface ILastDayToPlantAPI
{
    /// <summary>
    /// Gets the days needed for a specific crop for to grow.
    /// </summary>
    /// <param name="profession">Profession.</param>
    /// <param name="fertilizer">Int ID of fertilizer.</param>
    /// <param name="crop">crop ID.</param>
    /// <returns>number of days, or null for no entry.</returns>
    /// <remarks>This is not calculated until a Low priority DayStarted. You'll need an even lower priority.</remarks>
    public int? GetDays(Profession profession, int fertilizer, int crop);
}

/// <summary>
/// A enum corresponding to the profession to check.
/// </summary>
public enum Profession
{
    None,
    Agriculturalist,
    Prestiged,
}