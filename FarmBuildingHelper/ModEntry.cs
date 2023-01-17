using FarmBuildingHelper.Framework;

using StardewModdingAPI.Events;

using StardewValley.Menus;

using AtraUtils = AtraShared.Utils.Utils;

namespace FarmBuildingHelper;

/// <inheritdoc />
internal sealed class ModEntry : Mod
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
        I18n.Init(helper.Translation);
        ModMonitor = this.Monitor;
        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);
        this.Monitor.Log($"Starting up: {this.ModManifest.UniqueID} - {typeof(ModEntry).Assembly.FullName}");

        helper.Events.Display.MenuChanged += this.OnMenuChanged;
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.NewMenu is CarpenterMenu or PurchaseAnimalsMenu)
        {
            ModMonitor.Log("Menu added", LogLevel.Alert);
        }
        else if (e.OldMenu is CarpenterMenu or PurchaseAnimalsMenu)
        {
            ModMonitor.Log("Menu left", LogLevel.Alert);
        }
    }
}
