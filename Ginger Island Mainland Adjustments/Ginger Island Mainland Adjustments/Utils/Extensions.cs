using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using NotNullAttribute = System.Diagnostics.CodeAnalysis.NotNullAttribute;

namespace GingerIslandMainlandAdjustments.Utils;

/// <summary>
/// Add some python-esque methods to the dictionaries.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// equivalent to python's dictionary.update().
    /// </summary>
    /// <typeparam name="TKey">Type of key.</typeparam>
    /// <typeparam name="TValue">Type of value.</typeparam>
    /// <param name="dictionary">Dictionary to update.</param>
    /// <param name="updateDict">Dictionary containing values to add to the first dictionary.</param>
    /// <returns>the dictionary (for chaining).</returns>
    public static IDictionary<TKey, TValue> Update<TKey, TValue>(
        [NotNull] this Dictionary<TKey, TValue> dictionary,
        IDictionary<TKey, TValue>? updateDict)
        where TKey : notnull
        where TValue : notnull
    {
        if (updateDict is not null)
        {
            foreach (TKey key in updateDict.Keys)
            {
                dictionary[key] = updateDict[key];
            }
        }
        return dictionary;
    }

    /// <summary>
    /// equivalent to python's dictionary.setdefault().
    /// </summary>
    /// <typeparam name="TKey">Type of key.</typeparam>
    /// <typeparam name="TValue">Type of value.</typeparam>
    /// <param name="dictionary">Dictionary.</param>
    /// <param name="key">Key to look for.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <returns>Value from dictionary if one exists, else default value.</returns>
    /// <remarks>Function both sets state and returns value.</remarks>
    public static TValue? SetDefault<TKey, TValue>(
        [NotNull] this IDictionary<TKey, TValue> dictionary,
        [NotNull] TKey key,
        [NotNull] TValue defaultValue)
        where TKey : notnull
        where TValue : notnull
    {
        // add the value to the dictionary if it doesn't exist.
        dictionary.TryAdd(key, defaultValue);
        return dictionary[key];
    }

    /// <summary>
    /// similar to SetDefault, but will override a null value.
    /// </summary>
    /// <typeparam name="TKey">Type of key.</typeparam>
    /// <typeparam name="TValue">Type of value.</typeparam>
    /// <param name="dictionary">Dictionary to search in.</param>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">Value to use.</param>
    /// <returns>Value from dictionary if it exists and is not null, defaultValue otherwise.</returns>
    public static TValue SetDefaultOverrideNull<TKey, TValue>(
        [NotNull] this IDictionary<TKey, TValue> dictionary,
        [NotNull] TKey key,
        [NotNull] TValue defaultValue)
        where TKey : notnull
        where TValue : notnull
    {
        if (dictionary.TryGetValue(key, out TValue? value) && value is not null)
        {
            return value;
        }
        else
        {
            dictionary[key] = defaultValue;
            return defaultValue;
        }
    }

    /// <summary>
    /// Retrieves a value from the dictionary.
    /// Uses the default if the value is null, or if the key is not found.
    /// </summary>
    /// <typeparam name="TKey">Type of key.</typeparam>
    /// <typeparam name="TValue">Type of value.</typeparam>
    /// <param name="dictionary">Dictionary to search in.</param>
    /// <param name="key">Key.</param>
    /// <param name="defaultValue">Default value.</param>
    /// <returns>Value from dictionary if not null, or else defaultValue.</returns>
    [Pure]
    public static TValue GetValueOrDefaultOverrideNull<TKey, TValue>(
        [NotNull] this IDictionary<TKey, TValue> dictionary,
        [NotNull] TKey key,
        [NotNull] TValue defaultValue)
      where TKey : notnull
      where TValue : notnull
    {
        if (dictionary.TryGetValue(key, out TValue? value) && value is not null)
        {
            return value;
        }
        else
        {
            return defaultValue;
        }
    }
}

/// <summary>
/// Adds some LINQ-esque methods to the Regex class.
/// </summary>
public static class RegexExtensions
{
    /// <summary>
    /// Converts a Match with named matchgroups into a dictionary.
    /// </summary>
    /// <param name="match">Regex matchgroup.</param>
    /// <returns>Dictionary with the name of the matchgroup as the key and the value as the value.</returns>
    [Pure]
    public static Dictionary<string, string> MatchGroupsToDictionary([NotNull] this Match match)
    {
        Dictionary<string, string> result = new();
        foreach (Group group in match.Groups)
        {
            result[group.Name] = group.Value;
        }
        return result;
    }

    /// <summary>
    /// Converts a Match with named matchgroups into a dictionary.
    /// </summary>
    /// <typeparam name="TKey">Type for key.</typeparam>
    /// <typeparam name="TValue">Type for value.</typeparam>
    /// <param name="match">Match with named matchgroups.</param>
    /// <param name="keyselector">Function to apply to all keys.</param>
    /// <param name="valueselector">Function to apply to all values.</param>
    /// <returns>The dictionary.</returns>
    [Pure]
    public static Dictionary<TKey, TValue> MatchGroupsToDictionary<TKey, TValue>(
        [NotNull] this Match match,
        [NotNull] Func<string, TKey> keyselector,
        [NotNull] Func<string, TValue> valueselector)
        where TKey : notnull
        where TValue : notnull
    {
        Dictionary<TKey, TValue> result = new();
        foreach (Group group in match.Groups)
        {
            result[keyselector(group.Name)] = valueselector(group.Value);
        }
        return result;
    }
}

/// <summary>
/// Extension methods for SMAPI's logging service.
/// </summary>
internal static class LogExtensions
{
    /// <summary>
    /// Logs to level (DEBUG by default) if compiled with the DEBUG flag
    /// Logs to verbose otherwise.
    /// </summary>
    /// <param name="monitor">SMAPI's logger.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="level">Level to log at.</param>
    public static void DebugLog(
        [NotNull] this IMonitor monitor,
        [NotNull] string message,
        [NotNull] LogLevel level = LogLevel.Debug)
    {
#if DEBUG
        monitor.Log(message, level);
#else
        monitor.VerboseLog(message);
#endif
    }
}

