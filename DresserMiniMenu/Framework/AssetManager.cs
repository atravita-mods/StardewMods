using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

namespace DresserMiniMenu.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static IAssetName iconName = null!;
    private static string directoryPath = null!;

    private static Lazy<Texture2D> icons = new(() => Game1.content.Load<Texture2D>(iconName.BaseName));

    /// <summary>
    /// Gets the texture used for our icons.
    /// </summary>
    internal static Texture2D Icons => icons.Value;

    /// <summary>
    /// Initializes this asset manager.
    /// </summary>
    /// <param name="parser">game content helper.</param>
    /// <param name="directoryPath">The current directory path of this mod.</param>
    internal static void Initialize(IGameContentHelper parser, string directoryPath)
    {
        iconName = parser.ParseAssetName("Mods/atravita/DresserMiniMenu/Icons");
        AssetManager.directoryPath = directoryPath;
    }

    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(iconName))
        {
            string filename = "assets/icons.png";
            if (Game1.content.GetCurrentLanguage() is not LocalizedContentManager.LanguageCode.en)
            {
                string localeFilename;
                LocalizedContentManager.LanguageCode locale = Game1.content.GetCurrentLanguage();
                if (locale != LocalizedContentManager.LanguageCode.mod)
                {
                    localeFilename = $"assets/icons.{LocalizedContentManager.LanguageCodeString(locale)}.png";
                }
                else
                {
                    localeFilename = $"assets/icons.{LocalizedContentManager.CurrentModLanguage.LanguageCode}.png";
                }
                if (File.Exists(Path.Combine(directoryPath, localeFilename)))
                {
                    filename = localeFilename;
                }
            }
            e.LoadFromModFile<Texture2D>(filename, AssetLoadPriority.Exclusive);
        }
    }

    /// <inheritdoc cref="IContentEvents.AssetsInvalidated"/>
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (icons.IsValueCreated && (assets is null || assets.Contains(iconName)))
        {
            icons = new(() => Game1.temporaryContent.Load<Texture2D>(iconName.BaseName));
        }
    }
}
