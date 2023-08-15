#if DEBUG
using System.Diagnostics;
#endif

using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.Internal;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.Utils.Extensions;

using DresserMiniMenu.Framework;

using HarmonyLib;

using StardewModdingAPI.Events;

using AtraUtils = AtraShared.Utils.Utils;

namespace DresserMiniMenu;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <summary>
    /// Gets the config instance for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <summary>
    /// Gets a reference to JA's api.
    /// </summary>
    internal static IJsonAssetsAPI? JaAPI { get; private set; }

    /// <summary>
    /// Gets a list of shirt IDs registered to JA, or null if not found.
    /// </summary>
    internal static int[]? ShirtIDs { get; private set; }

    /// <summary>
    /// Gets a list of pant IDs registered to JA, or null if not found.
    /// </summary>
    internal static int[]? PantsIDs { get; private set; }

    internal static int[]? HatIDs { get; private set; }

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        base.Entry(helper);
        I18n.Init(helper.Translation);
        AssetManager.Initialize(helper.GameContent, helper.DirectoryPath);
        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunch;
#if DEBUG
        Stopwatch sw = Stopwatch.StartNew();
#endif
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
#if DEBUG
        this.Monitor.LogTimespan("Applying harmony patches", sw);
#endif

        this.Helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Apply(e);
        this.Helper.Events.Content.AssetsInvalidated += static (_, e) => AssetManager.Reset(e.NamesWithoutLocale);
        this.Helper.Events.Content.LocaleChanged += static (_, _) => AssetManager.Reset();
        this.Helper.Events.GameLoop.DayEnding += static (_, _) => AssetManager.Reset();

        this.Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll(typeof(ModEntry).Assembly);
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    private void OnGameLaunch(object? sender, GameLaunchedEventArgs e)
    {
        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (helper.TryGetAPI())
        {
            helper.Register(
                reset: static () => Config = new(),
                save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config))
            .AddParagraph(I18n.ModDescription)
            .GenerateDefaultGMCM(static () => Config);
        }

        {
            IntegrationHelper optional = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, LogLevel.Trace);
            if (optional.TryGetAPI("spacechase0.JsonAssets", "1.10.10", out IJsonAssetsAPI? jaAPI))
            {
                JaAPI = jaAPI;
            }
        }
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        HatIDs = JaAPI?.GetAllHatIds().Values.ToArray();

        if (JaAPI?.GetAllClothingIds() is IDictionary<string, int> clothing)
        {
            List<int> shirts = new();
            List<int> pants = new();

            foreach (var id in clothing.Values)
            {
                if (!Game1.clothingInformation.TryGetValue(id, out var data))
                {
                    continue;
                }
                ReadOnlySpan<char> type = data.GetNthChunk('/', 8).Trim();
                if (type.Length == 0)
                {
                    continue;
                }

                if (type.Equals("shirt", StringComparison.OrdinalIgnoreCase))
                {
                    shirts.Add(id);
                }
                else if (type.Equals("pants", StringComparison.OrdinalIgnoreCase))
                {
                    pants.Add(id);
                }
            }

            ShirtIDs = shirts.Count == 0 ? null : shirts.ToArray();
            PantsIDs = pants.Count == 0 ? null : pants.ToArray();
        }

        this.Monitor.Log($"Indexed {HatIDs?.Length ?? 0} hats, {ShirtIDs?.Length ?? 0} shirts, and {PantsIDs?.Length ?? 0} pants.");
    }
}
