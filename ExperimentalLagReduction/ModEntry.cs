using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using ExperimentalLagReduction.Framework;
using ExperimentalLagReduction.HarmonyPatches;

using HarmonyLib;
using StardewModdingAPI.Events;

using AtraUtils = AtraShared.Utils.Utils;

namespace ExperimentalLagReduction;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // initialize translations.
        I18n.Init(helper.Translation);

        // statics
        ModMonitor = this.Monitor;
        this.Monitor.Log($"Starting up: {this.ModManifest.UniqueID} - {typeof(ModEntry).Assembly.FullName}");
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunch;

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.ConsoleCommands.Add("av.elr.print_report", "todo", this.Report);
        helper.ConsoleCommands.Add("av.elr.get_path_from", "todo", this.GetPath);
        helper.ConsoleCommands.Add("av.elr.dump_cache", "todo", static (_, _) => Rescheduler.PrintCache());
    }

    private void GetPath(string arg1, string[] args)
    {
        if (args.Length != 3)
        {
            ModMonitor.Log("Expected three arguments", LogLevel.Error);
        }
        if (!int.TryParse(args[2], out int gender) || gender < 0 || gender > 2)
        {
            ModMonitor.Log("Expected int gender", LogLevel.Error);
        }
        var path = Rescheduler.GetPathFromCache(args[0], args[1], gender);
        if (path is not null)
        {
            ModMonitor.Log(string.Join("->", path), LogLevel.Info);
        }
        else
        {
            ModMonitor.Log($"That path was not cached.", LogLevel.Info);
        }
    }

    private void Report(string command, string[] args)
    {
        ModMonitor.Log($"Total locations: {Game1.locations.Count}", LogLevel.Info);
        ModMonitor.Log($"Cached routes: {Rescheduler.CacheCount}", LogLevel.Info);
    }

    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
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
            harmony.PatchAll(typeof(ModEntry).Assembly);
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }
}
