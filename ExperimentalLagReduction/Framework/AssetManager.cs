using StardewModdingAPI.Events;

namespace ExperimentalLagReduction.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static IAssetName reschedulerPopulate = null!;

    /// <summary>
    /// Initializes this asset manager.
    /// </summary>
    /// <param name="parser">Game content parser.</param>
    internal static void Initialize(IGameContentHelper parser)
        => reschedulerPopulate = parser.ParseAssetName("Mods/atravita/Rescheduler_Populate");

    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo(reschedulerPopulate))
        {
            e.LoadFrom(static () => new Dictionary<string, string>() { ["Town"] = "3", ["Mountain"] = "2", ["Forest"] = "2", ["IslandSouth"] = "2" }, AssetLoadPriority.Exclusive);
        }
    }

    /// <summary>
    /// Gets the dictionary of locations to pre-populate for.
    /// </summary>
    /// <returns>Location->radius dictionary.</returns>
    internal static Dictionary<string, string> GetPrepopulate() => Game1.content.Load<Dictionary<string, string>>(reschedulerPopulate.BaseName);
}
