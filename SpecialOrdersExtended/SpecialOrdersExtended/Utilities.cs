using System.Globalization;

namespace SpecialOrdersExtended;

internal class Utilities
{

    /// <summary>
    /// Sort strings, taking into account CultureInfo
    /// </summary>
    /// <param name="enumerable"></param>
    /// <returns></returns>
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
