using System.Reflection;
using NotNullAttribute = System.Diagnostics.CodeAnalysis.NotNullAttribute;

namespace StopRugRemoval;

/// <summary>
/// Small extensions to get the full name of a method.
/// </summary>
internal static class MethodExtensions
{
    /// <summary>
    /// Gets the fully qualified name of a method.
    /// </summary>
    /// <param name="method">MethodBase to analyze.</param>
    /// <returns>Fully qualified name.</returns>
    [Pure]
    public static string GetFullName([NotNull] this MethodBase method) => $"{method.DeclaringType}::{method.Name}";

    /// <summary>
    /// Gets the fully qualified name of a method.
    /// </summary>
    /// <param name="method">MethodInfo to analyze.</param>
    /// <returns>Fully qualified name.</returns>
    [Pure]
    public static string GetFullName([NotNull] this MethodInfo method) => $"{method.DeclaringType}::{method.Name}";
}
