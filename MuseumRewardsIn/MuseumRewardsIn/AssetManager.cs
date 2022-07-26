using AtraBase.Collections;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace MuseumRewardsIn;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Preference.")]
internal static class AssetManager
{
    private static readonly string LETTERS_TO_CHECK = PathUtilities.NormalizePath("Mods/atravita/MuseumStore/Letters");

    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(LETTERS_TO_CHECK))
        {
            e.LoadFrom(EmptyContainers.GetEmptyDictionary<string, string>, AssetLoadPriority.Low);
        }
    }

    internal static HashSet<string> GetMailFlagsForStore()
        => Game1.temporaryContent.Load<Dictionary<string, string>>(LETTERS_TO_CHECK).Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
}
