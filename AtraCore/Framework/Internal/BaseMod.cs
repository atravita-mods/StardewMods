namespace AtraCore.Framework.Internal;

// this little generic trickery means that each mod gets separate statics.

/// <inheritdoc />
public abstract class BaseMod<T> : Mod
    where T : Mod
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