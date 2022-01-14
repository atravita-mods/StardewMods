using System.Globalization;

namespace SpecialOrdersExtended;

/// <summary>
/// Utility class, contains small functions that are generally helpful.
/// </summary>
internal class Utilities
{
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
