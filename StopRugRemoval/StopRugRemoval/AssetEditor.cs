﻿using AtraBase.Toolkit.Extensions;

using AtraShared.Caching;
using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace StopRugRemoval;

/// <summary>
/// Handles editing assets.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1214:Readonly fields should appear before non-readonly fields", Justification = "Reviewed.")]
internal static class AssetEditor
{
    private static IAssetName saloonEvents = null!;
    private static IAssetName betIconsPath = null!;
    private static Lazy<Texture2D> betIconLazy = new(static () => Game1.content.Load<Texture2D>(betIconsPath.BaseName));

    private static readonly PerScreen<TickCache<bool>> HasSeenSaloonEvent = new(
        () => new (static () => Game1.player?.eventsSeen?.Contains(40) == true));

    private static IAssetName weapons = null!;

    #region birdiequest

    private static readonly Dictionary<IAssetName, int> BirdieQuest = new();

    private static LocalizedContentManager? contentManager;

    #endregion

    /// <summary>
    /// Gets the bet button textures.
    /// </summary>
    internal static Texture2D BetIcon => betIconLazy.Value;

    /// <summary>
    /// Initializes assets for this mod.
    /// </summary>
    /// <param name="parser">GameContentHelper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        saloonEvents = parser.ParseAssetName("Data/Events/Saloon");
        betIconsPath = parser.ParseAssetName("Mods/atravita_StopRugRemoval_BetIcons");
        weapons = parser.ParseAssetName("Data/weapons");

        const string dialogue = "Characters/Dialogue/";
        BirdieQuest.Add(parser.ParseAssetName($"{dialogue}Kent"), 864);
        BirdieQuest.Add(parser.ParseAssetName($"{dialogue}Gus"), 865);
        BirdieQuest.Add(parser.ParseAssetName($"{dialogue}Sandy"), 866);
        BirdieQuest.Add(parser.ParseAssetName($"{dialogue}George"), 867);
        BirdieQuest.Add(parser.ParseAssetName($"{dialogue}Wizard"), 868);
        BirdieQuest.Add(parser.ParseAssetName($"{dialogue}Willy"), 869);
    }

    /// <summary>
    /// Disposes the content manager.
    /// </summary>
    internal static void Dispose()
    {
        contentManager?.Dispose();
        contentManager = null;
    }

    /// <summary>
    /// Refreshes lazies.
    /// </summary>
    /// <param name="assets">IReadOnlySet of assets to refresh.</param>
    internal static void Refresh(IReadOnlySet<IAssetName>? assets = null)
    {
        if (betIconLazy.IsValueCreated && (assets is null || assets.Contains(betIconsPath)))
        {
            betIconLazy = new(static () => Game1.content.Load<Texture2D>(betIconsPath.BaseName));
        }
    }

    /// <summary>
    /// Applies edits.
    /// </summary>
    /// <param name="e">Event args.</param>
    /// <param name="directoryPath">The absolute path to the mod.</param>
    internal static void Edit(AssetRequestedEventArgs e, string directoryPath)
    {
        if (BirdieQuest.TryGetValue(e.NameWithoutLocale, out int id))
        {
            e.Edit(
                (asset) =>
                {
                    string key = $"accept_{id}";
                    IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
                    if (!data.ContainsKey(key))
                    {
                        string character = e.NameWithoutLocale.BaseName.GetNthChunk('/', 2).ToString();
                        ModEntry.ModMonitor.LogOnce($"Found NPC {character} missing Birdie quest dialogue key {key}. This is likely because you installed an older dialogue mod that is replacing all of this charcter's dialogue. This may cause issues.", LogLevel.Warn);
                        contentManager ??= new(Game1.content.ServiceProvider, Game1.content.RootDirectory);
                        try
                        {
                            Dictionary<string, string> original = contentManager.LoadBase<Dictionary<string, string>>(e.NameWithoutLocale.BaseName);
                            if (original.TryGetValue(key, out string? original_dialogue))
                            {
                                ModEntry.ModMonitor.Log($"Original dialogue key {key} found: {original_dialogue}. Adding back", LogLevel.Info);
                                data[key] = original_dialogue;
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            ModEntry.ModMonitor.Log($"Could not find original dialogue for {character}:\n\n{ex}");
                        }
                        ModEntry.ModMonitor.Log($"Could not restore birdie quest key for {character}.", LogLevel.Warn);
                    }
                },
                AssetEditPriority.Late + 1000);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(weapons))
        {
            e.Edit(
                static (asset) =>
                {
                    ModEntry.ModMonitor.DebugOnlyLog("Checking weapons");
                    IDictionary<int, string> data = asset.AsDictionary<int, string>().Data;

                    // check golden scythe and infinity gavel.
                    if (!data.ContainsKey(53) || !data.ContainsKey(63))
                    {
                        ModEntry.ModMonitor.LogOnce("Missing weapons detected, are you using a weapons mod made before 1.5?", LogLevel.Error);
                        contentManager ??= new(Game1.content.ServiceProvider, Game1.content.RootDirectory);
                        try
                        {
                            Dictionary<int, string> original = contentManager.LoadBase<Dictionary<int, string>>(asset.NameWithoutLocale.BaseName);
                            foreach ((int key, string value) in original)
                            {
                                if (data.TryAdd(key, value))
                                {
                                    ModEntry.ModMonitor.LogOnce($"Restoring missing weapon: {key}", LogLevel.Info);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ModEntry.ModMonitor.Log($"Could not find original weapons file:\n\n{ex}");
                        }
                    }
                },
                AssetEditPriority.Late + 1000);
        }
        else if (Context.IsWorldReady && e.NameWithoutLocale.IsEquivalentTo(betIconsPath))
        { // The BET1k/10k icons have to be localized, so they're in the i18n folder.
            string filename = "BetIcons.png";

            if (Game1.content.GetCurrentLanguage() is not LocalizedContentManager.LanguageCode.en)
            {
                string localeFilename;
                LocalizedContentManager.LanguageCode locale = Game1.content.GetCurrentLanguage();
                if (locale != LocalizedContentManager.LanguageCode.mod)
                {
                    localeFilename = $"BetIcons.{Game1.content.LanguageCodeString(locale)}.png";
                }
                else
                {
                    localeFilename = $"BetIcons.{LocalizedContentManager.CurrentModLanguage.LanguageCode}.png";
                }
                if (File.Exists(Path.Combine(directoryPath, "i18n", localeFilename)))
                {
                    filename = localeFilename;
                }
            }
            e.LoadFromModFile<Texture2D>(Path.Combine("i18n", filename), AssetLoadPriority.Low);
        }
    }

    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    /// <remarks>
    /// Handles editing the saloon event to give the player a choice about alcohol.
    /// </remarks>
    internal static void EditSaloonEvent(AssetRequestedEventArgs e)
    {
        if (ModEntry.Config.Enabled && ModEntry.Config.EditElliottEvent && !HasSeenSaloonEvent.Value.GetValue() && e.NameWithoutLocale.IsEquivalentTo(saloonEvents))
        {
            e.Edit(EditSaloonImpl, AssetEditPriority.Late);
        }
    }

    /// <inheritdoc cref="AssetRequestedEventArgs.Edit(Action{IAssetData}, AssetEditPriority, string?)"/>
    private static void EditSaloonImpl(IAssetData asset)
    {
        IAssetDataForDictionary<string, string>? editor = asset.AsDictionary<string, string>();

        if (editor.Data.ContainsKey("atravita_elliott_nodrink"))
        {// event has been edited already?
            return;
        }
        foreach ((string key, string value) in editor.Data)
        {
            if (key.StartsWith("40/", StringComparison.OrdinalIgnoreCase))
            {
                int index = value.IndexOf("speak Elliott");
                int second = value.IndexOf("speak Elliott", index + 1);
                int nextslash = value.IndexOf('/', second);
                if (nextslash > -1)
                {
                    string initial = value[..nextslash] + $"/question fork1 \"#{I18n.Drink()}#{I18n.Nondrink()}\"/fork atravita_elliott_nodrink/";
                    string remainder = value[(nextslash + 1)..];

                    editor.Data["atravita_elliott_nodrink"] = remainder.Replace("346", "350");
                    editor.Data[key] = initial + remainder;
                }
                return;
            }
        }
    }
}