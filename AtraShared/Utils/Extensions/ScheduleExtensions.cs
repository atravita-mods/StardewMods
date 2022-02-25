namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extension methods on Stardew's SchedulePathDescription class.
/// </summary>
internal static class SchedulePointDescriptionExtensions
{
    /// <summary>
    /// Gets the expected travel time of a SchedulePathDescription.
    /// </summary>
    /// <param name="schedulePathDescription">Schedule Path Description.</param>
    /// <returns>Time in in-game minutes, not rounded.</returns>
    [Pure]
    public static int GetExpectedRouteTime([NotNull] this SchedulePathDescription schedulePathDescription)
    {
        return schedulePathDescription.route.Count * 32 / 42;
    }
}