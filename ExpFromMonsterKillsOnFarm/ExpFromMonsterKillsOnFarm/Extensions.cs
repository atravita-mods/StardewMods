using System.Reflection;

namespace ExpFromMonsterKillsOnFarm;

/// <summary>
/// Small extensions to get the full name of a method.
/// </summary>
internal static class MethodExtensions
{
    /// <summary>
    /// Gets the full name of a MethodBase.
    /// </summary>
    /// <param name="method">MethodBase to analyze.</param>
    /// <returns>Fully qualified name of a MethodBase.</returns>
    [Pure]
    public static string GetFullName([NotNull] this MethodBase method) => $"{method.DeclaringType}::{method.Name}";

    /// <summary>
    /// Gets the full name of a MethodInfo.
    /// </summary>
    /// <param name="method">MethodInfo to analyze.</param>
    /// <returns>Fully qualified name of a MethodInfo.</returns>
    [Pure]
    public static string GetFullName([NotNull] this MethodInfo method) => $"{method.DeclaringType}::{method.Name}";
}