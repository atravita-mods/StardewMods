using StardewModdingAPI.Events;

namespace AtraCore.Framework.Internal;

/// <inheritdoc />
public abstract class BaseMod : Mod
{
    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    public static IMonitor ModMonitor { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;
        this.Monitor.Log($"Starting up: {this.ModManifest.UniqueID} - {this.GetType().Assembly.FullName}");
    }
}