using AtraCore.Framework.ReflectionManager;
using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using StardewModdingAPI.Events;

namespace ExperimentalLagReduction;

/// <inheritdoc />
internal class ModEntry : Mod
{
    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;


    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // initialize translations.
        I18n.Init(helper.Translation);

        // statics
        ModMonitor = this.Monitor;

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunch;
    }

    private void OnGameLaunch(object? sender, GameLaunchedEventArgs e)
    {
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
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
