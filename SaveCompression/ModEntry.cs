using HarmonyLib;

namespace SaveCompression;

/// <inheritdoc/>
internal sealed class ModEntry : Mod
{

    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModMonitor = this.Monitor;
        new Harmony(this.ModManifest.UniqueID).PatchAll(typeof(ModEntry).Assembly);
    }
}
