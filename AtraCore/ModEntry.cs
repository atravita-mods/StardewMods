using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;
using AtraBase.Toolkit.StringHandler;
using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using StardewModdingAPI.Events;
using StardewValley.Menus;

namespace AtraCore;

/// <inheritdoc />
internal class ModEntry : Mod
{
    internal static IMonitor ModMonitor = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModMonitor = this.Monitor;

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

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
        harmony.Snitch(this.Monitor, uniqueID: harmony.Id, transpilersOnly: true);
    }
}
