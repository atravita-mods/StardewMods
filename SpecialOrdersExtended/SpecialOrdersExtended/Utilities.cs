using System.Globalization;

namespace SpecialOrdersExtended;

/// <summary>
/// Utility class, contains small functions that are generally helpful.
/// </summary>
internal class Utilities
{
    /// <summary>
    /// Finalizes GetSpecialOrder to return null of there's an error.
    /// </summary>
    /// <param name="key">Key of the special order.</param>
    /// <param name="generation_seed">Random generation seed for the special order.</param>
    /// <param name="__result">The parsed special order, set to null to remove.</param>
    /// <param name="__exception">The observed exception.</param>
    /// <returns>null to surpress the error.</returns>
    public static Exception? FinalizeGetSpecialOrder(string key, int? generation_seed, ref SpecialOrder? __result, Exception? __exception)
    {
        if (__exception is not null)
        {
            ModEntry.ModMonitor.Log($"Detected invalid special order {key}\n\n{__exception}", LogLevel.Error);
            __result = null;
        }
        return null;
    }

    /// <summary>
    /// Sort strings, taking into account CultureInfo of currently selected language.
    /// </summary>
    /// <param name="enumerable">IEnumerable of strings to sort.</param>
    /// <returns>A sorted list of strings.</returns>
    [Pure]
    public static List<string> ContextSort(IEnumerable<string> enumerable)
    {
        LocalizedContentManager contextManager = Game1.content;
        string langcode = contextManager.LanguageCodeString(contextManager.GetCurrentLanguage());
        List<string> outputlist = enumerable.ToList();
        outputlist.Sort(StringComparer.Create(new CultureInfo(langcode), true));
        return outputlist;
    }


}
