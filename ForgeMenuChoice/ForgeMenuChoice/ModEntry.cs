using AtraCore.Framework.Internal;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;

using ForgeMenuChoice.HarmonyPatches;

using HarmonyLib;

using StardewModdingAPI.Events;

using AtraUtils = AtraShared.Utils.Utils;

namespace ForgeMenuChoice;

/// <inheritdoc/>
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <summary>
    /// Gets the translation helper for this mod.
    /// </summary>
    internal static ITranslationHelper TranslationHelper { get; private set; } = null!;

    /// <summary>
    /// Gets the configuration class for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <summary>
    /// Gets the input helper for this mod.
    /// </summary>
    internal static IInputHelper InputHelper { get; private set; } = null!;

    /// <summary>
    /// Gets the string utilities for this mod.
    /// </summary>
    internal static StringUtils StringUtils { get; private set; } = null!;

    /// <summary>
    /// Gets a delegate that checks to see if the forge instance is Casey's NewForgeMenu or not.
    /// </summary>
    internal static Func<object, bool>? IsSpaceForge { get; private set; } = null;

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        base.Entry(helper);

        StringUtils = new(this.Monitor);
        TranslationHelper = helper.Translation;
        InputHelper = helper.Input;

        AssetLoader.Initialize(helper.GameContent);
        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        helper.Events.Player.Warped += this.Player_Warped;
        helper.Events.Content.AssetRequested += static (_, e) => AssetLoader.OnLoadAsset(e);
        helper.Events.Content.LocaleChanged += this.OnLocaleChanged;
        helper.Events.Content.AssetsInvalidated += static (_, e) => AssetLoader.Refresh(e.NamesWithoutLocale);

        helper.Events.Input.ButtonsChanged += static (_, e) => ForgeMenuPatches.ApplyButtonPresses(e);
    }

    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    /// <remarks>We must wait until GameLaunched to patch in order to patch Spacecore.</remarks>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (helper.TryGetAPI())
        {
            helper.Register(
                reset: static () => Config = new ModConfig(),
                save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config))
            .AddParagraph(I18n.ModDescription)
            .GenerateDefaultGMCM(static () => Config);
        }
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll(typeof(ModEntry).Assembly);

            if (this.Helper.ModRegistry.Get("Goldenrevolver.EnchantableScythes") is IModInfo sycthes)
            {
                this.Monitor.Log("Applying compat patches for Enchantable Scythes.", LogLevel.Debug);
                GetEnchantmentPatch.ApplyPatch(harmony);
            }
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    #region assets

    private void Player_Warped(object? sender, WarpedEventArgs e)
    {
        if (e.IsLocalPlayer)
        {
            AssetLoader.Refresh();
        }
    }

    private void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
    {
        this.Helper.GameContent.InvalidateCacheAndLocalized(AssetLoader.ENCHANTMENT_NAMES_LOCATION);

        AssetLoader.Refresh();
    }

    #endregion
}
