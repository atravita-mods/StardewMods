using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace MuseumRewardsIn;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Preference.")]
internal static class AssetManager
{
    private static readonly string LETTERS_TO_CHECK = PathUtilities.NormalizePath("Mods/atravita/MuseumStore/Letters");

    private static IAssetName? letters;

    private static Lazy<HashSet<string>> mailflags = new(GetMailFlagsForStore);

    private static IAssetName Letters =>
        letters ??= ModEntry.GameContentHelper.ParseAssetName(LETTERS_TO_CHECK);

    internal static HashSet<string> MailFlags => mailflags.Value;

    internal static void Invalidate(IReadOnlySet<IAssetName>? names = null)
    {
        if (mailflags.IsValueCreated && (names is null || names.Contains(Letters)))
        {
            mailflags = new(GetMailFlagsForStore);
        }
    }

    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(LETTERS_TO_CHECK))
        {
            e.LoadFromModFile<Dictionary<string, string>>("assets/vanilla_mail.json", AssetLoadPriority.Exclusive);
        }
    }

    private static HashSet<string> GetMailFlagsForStore()
        => Game1.temporaryContent.Load<Dictionary<string, string>>(LETTERS_TO_CHECK).Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
}
