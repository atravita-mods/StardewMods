using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;

using AtraUtils = AtraShared.Utils.Utils;

namespace ForgeMenuChoice;

public class AssetLoader: IAssetLoader
{
    private const string ASSETPREFIX = "Mods/atravita_ForgeMenuChoice_";

    private static string UI_ELEMENT_LOCATION = PathUtilities.NormalizeAssetName("assets/Forge-Buttons.png");
    private static string UI_ASSET_PATH = PathUtilities.NormalizeAssetName(ASSETPREFIX + "Forge_Buttons");
    private static string TOOLTIP_DATA_PATH = PathUtilities.NormalizeAssetName(ASSETPREFIX + "Tooltip_Data");

    private static Lazy<Texture2D> UIElementLazy = new(() => ModEntry.ContentHelper.Load<Texture2D>(UI_ASSET_PATH, ContentSource.GameContent));
    private static Lazy<Dictionary<string, string>> TooltipDataLazy = new(() => ModEntry.ContentHelper.Load<Dictionary<string, string>>(TOOLTIP_DATA_PATH, ContentSource.GameContent));

    /// <summary>
    /// Gets the instance of the AssetLoader.
    /// </summary>
    public static AssetLoader Instance { get; } = new();

    /// <summary>
    /// Gets the textures for the UI elements used by this mod.
    /// </summary>
    internal static Texture2D UIElement => UIElementLazy.Value;

    /// <summary>
    /// Gets a dictionary for the tooltip data.
    /// </summary>
    internal static Dictionary<string, string> TooltipData => TooltipDataLazy.Value;

    /// <inheritdoc/>
    [UsedImplicitly]
    public bool CanLoad<T>(IAssetInfo asset)
        => asset.AssetNameEquals(UI_ASSET_PATH) || asset.AssetNameEquals(TOOLTIP_DATA_PATH);

    /// <inheritdoc/>
    [UsedImplicitly]
    public T Load<T>(IAssetInfo asset)
    {
        if (asset.AssetNameEquals(UI_ASSET_PATH))
        {
            return (T)(object)ModEntry.ContentHelper.Load<Texture2D>(UI_ELEMENT_LOCATION);
        }
        else if (asset.AssetNameEquals(TOOLTIP_DATA_PATH))
        {
            Dictionary<string, string> tooltipdata = new();

            // Do nothing if the world is not ready yet.
            // For some reason trying to load the translations too early jacks them up
            // and suddenly enchantments aren't translated in other languages.
            if (!ModEntry.Config.EnableTooltipAutogeneration || Context.IsWorldReady)
            {
                return (T)(object)tooltipdata;
            }

            // the journal scrap 1008 is the only in-game descriptions of enchantments. We'll need to grab data from there.
            try
            {
                IDictionary<int, string> secretnotes = ModEntry.ContentHelper.Load<Dictionary<int, string>>("Data\\SecretNotes", ContentSource.GameContent);
                string[] secretNote8 = secretnotes[1008].Split("^^");

                // The secret note, of course, has its data in the localized name. We'll need to map that to the internal name.
                // Using a dictionary with a StringComparer for the user's current language to make that a little easier.
                Dictionary<string, string> tooltipmap = new(AtraUtils.GetCurrentLanguageComparer(ignoreCase: true));
                foreach (string str in secretNote8)
                {
                    string[] splits = str.Split(':', count: 2, options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (splits.Length >= 2)
                    {
                        tooltipmap[splits[0]] = splits[1];
                    }
                    ModEntry.ModMonitor.Log(splits[0], LogLevel.Info);
                }

                // For each enchantment, look up its description from the secret note and
                // and prepopulate the data file with that.
                foreach (BaseEnchantment enchantment in BaseEnchantment.GetAvailableEnchantments())
                {
                    if (tooltipmap.TryGetValue(enchantment.GetDisplayName(), out string? val))
                    {
                        tooltipdata[enchantment.GetName()] = val;
                    }
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Error: secret note 9 not found.\n\n{ex}", LogLevel.Error);
            }
            return (T)(object)tooltipdata;
        }
        throw new InvalidOperationException($"Should not have tried to load '{asset.AssetName}'");
    }

    /// <summary>
    /// Refreshes the Lazys.
    /// </summary>
    internal static void Refresh()
    {
        UIElementLazy = new(() => ModEntry.ContentHelper.Load<Texture2D>(UI_ASSET_PATH, ContentSource.GameContent));
        TooltipDataLazy = new(() => ModEntry.ContentHelper.Load<Dictionary<string, string>>(TOOLTIP_DATA_PATH, ContentSource.GameContent));
    }
}