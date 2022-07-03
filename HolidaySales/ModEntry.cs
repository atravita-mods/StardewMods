using AtraBase.Caching;
using AtraBase.Toolkit.Reflection;
using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using HolidaySales.HarmonyPatches;
using StardewModdingAPI.Events;

using AtraUtils = AtraShared.Utils.Utils;

namespace HolidaySales;

/// <inheritdoc />
internal class ModEntry : Mod
{
    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <summary>
    /// Gets the config instance for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // initialize translations.
        I18n.Init(helper.Translation);

        // statics
        ModMonitor = this.Monitor;
        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunch;
    }

    private void OnGameLaunch(object? sender, GameLaunchedEventArgs e)
    {
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (helper.TryGetAPI())
        {
            helper.Register(
                reset: static () => Config = new(),
                save: () => Task.Run(() => this.Helper.WriteConfig(Config)))
            .AddEnumOption(
                name: I18n.StoreFestivalBehavior_Title,
                getValue: static () => Config.StoreFestivalBehavior,
                setValue: static (value) => Config.StoreFestivalBehavior = value,
                tooltip: I18n.StoreFestivalBehavior_Description);
        }
    }

    /// <summary>
    /// Applies the patches for this mod.
    /// </summary>
    /// <param name="harmony">This mod's harmony instance.</param>
    /// <remarks>Delay until GameLaunched in order to patch other mods....</remarks>
    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }
}
