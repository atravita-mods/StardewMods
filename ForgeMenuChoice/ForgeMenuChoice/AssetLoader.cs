using AtraShared.Utils;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using AtraUtils = AtraShared.Utils.Utils;

namespace ForgeMenuChoice;

/// <summary>
/// Loads and manages assets used by this mod.
/// </summary>
public static class AssetLoader
{
    private const string ASSETPREFIX = "Mods/atravita_ForgeMenuChoice_";

#pragma warning disable SA1310 // Field names should not contain underscore. Reviewed.
    private static readonly string UI_ELEMENT_LOCATION = PathUtilities.NormalizeAssetName("assets/Forge-Buttons.png");
    private static readonly string UI_ASSET_PATH = PathUtilities.NormalizeAssetName(ASSETPREFIX + "Forge_Buttons");
    private static readonly string TOOLTIP_DATA_PATH = PathUtilities.NormalizeAssetName(ASSETPREFIX + "Tooltip_Data");
#pragma warning restore SA1310 // Field names should not contain underscore

    private static Lazy<Texture2D> uiElementLazy = new(() => ModEntry.GameContentHelper.Load<Texture2D>(UI_ASSET_PATH));
    private static Lazy<Dictionary<string, string>> tooltipDataLazy = new(GrabAndWrapTooltips);

    /// <summary>
    /// Gets the textures for the UI elements used by this mod.
    /// </summary>
    internal static Texture2D UIElement => uiElementLazy.Value;

    /// <summary>
    /// Gets a dictionary for the tooltip data.
    /// </summary>
    internal static Dictionary<string, string> TooltipData => tooltipDataLazy.Value;

    /// <summary>
    /// Gets the location of enchantment names.
    /// </summary>
    internal static string ENCHANTMENT_NAMES_LOCATION { get; } = PathUtilities.NormalizeAssetName("Strings/EnchantmentNames");

    /// <summary>
    /// Refreshes the Lazys.
    /// </summary>
    internal static void Refresh()
    {
        uiElementLazy = new(() => ModEntry.GameContentHelper.Load<Texture2D>(UI_ASSET_PATH));
        tooltipDataLazy = new(GrabAndWrapTooltips);
    }

    /// <summary>
    /// Handles loading assets from this mod.
    /// </summary>
    /// <param name="e">Event args.</param>
    internal static void OnLoadAsset(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(UI_ASSET_PATH))
        {
            e.LoadFromModFile<Texture2D>(UI_ELEMENT_LOCATION, AssetLoadPriority.Medium);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(TOOLTIP_DATA_PATH))
        {
            e.LoadFrom(GenerateToolTips, AssetLoadPriority.Medium);
        }
    }

    private static Dictionary<string, string> GenerateToolTips()
    {
        Dictionary<string, string> tooltipdata = new();

        // Do nothing if the world is not ready yet.
        // For some reason trying to load the translations too early jacks them up
        // and suddenly enchantments aren't translated in other languages.
        if (!ModEntry.Config.EnableTooltipAutogeneration || !Context.IsWorldReady)
        {
            return tooltipdata;
        }

        // the journal scrap 1008 is the only in-game descriptions of enchantments. We'll need to grab data from there.
        try
        {
            IDictionary<int, string> secretnotes = ModEntry.GameContentHelper.Load<Dictionary<int, string>>("Data\\SecretNotes");
            string[] secretNote8 = secretnotes[1008].Split("^^", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // The secret note, of course, has its data in the localized name. We'll need to map that to the internal name.
            // Using a dictionary with a StringComparer for the user's current language to make that a little easier.
            Dictionary<string, string> tooltipmap = new(AtraUtils.GetCurrentLanguageComparer(ignoreCase: true));
            foreach (string str in secretNote8)
            {
                string[] splits = str.Split(':', count: 2, options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (splits.Length < 2)
                {
                    // Chinese uses a different character to split by.
                    splits = str.Split('：', count: 2, options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }
                if (splits.Length >= 2)
                {
                    tooltipmap[splits[0]] = splits[1];
                }
            }

            // For each enchantment, look up its description from the secret note and
            // and prepopulate the data file with that.
            // Russian needs to be handled seperately.
            foreach (BaseEnchantment enchantment in BaseEnchantment.GetAvailableEnchantments())
            {
                if (ModEntry.TranslationHelper.Get("enchantment." + enchantment.GetName()) is Translation i18nname
                    && i18nname.HasValue())
                {
                    tooltipdata[enchantment.GetName()] = i18nname;
                }
                else if (tooltipmap.TryGetValue(enchantment.GetDisplayName(), out string? val))
                {
                    tooltipdata[enchantment.GetName()] = val;
                }
                else if (Game1.content.GetCurrentLanguage() == LocalizedContentManager.LanguageCode.ru)
                {
                    string[] splits = enchantment.GetDisplayName().Split();
                    foreach (string i in splits)
                    {
                        if (i != "чары" && tooltipmap.TryGetValue(i, out string? value))
                        {
                            tooltipdata[enchantment.GetName()] = value;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Error: journal scrap 9 not found.\n\n{ex}", LogLevel.Error);
        }
        return tooltipdata;
    }

    private static Dictionary<string, string> GrabAndWrapTooltips()
    {
        Dictionary<string, string> tooltips = ModEntry.GameContentHelper.Load<Dictionary<string, string>>(TOOLTIP_DATA_PATH);
        foreach ((string k, string v) in tooltips)
        {
            tooltips[k] = StringUtils.ParseAndWrapText(v, Game1.smallFont, 300);
        }
        return tooltips;
    }
}