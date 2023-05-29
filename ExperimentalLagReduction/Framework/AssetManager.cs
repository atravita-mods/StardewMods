using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AtraBase.Collections;

using StardewModdingAPI.Events;

namespace ExperimentalLagReduction.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static IAssetName reschedulerPopulate = null!;

    internal static void Initialize(IGameContentHelper parser)
        => reschedulerPopulate = parser.ParseAssetName("Mods/atravita/Rescheduler_Populate");
    
    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo(reschedulerPopulate))
        {
            e.LoadFrom(static () => new Dictionary<string, string>() { ["Town"] = "3", ["Mountain"] = "2", ["Forest"] = "2" }, AssetLoadPriority.Exclusive);
        }
    }

    /// <summary>
    /// Gets the dictionary of locations to pre-populate for.
    /// </summary>
    /// <returns>Location->radius dictionary.</returns>
    internal static Dictionary<string, string> GetPrepopulate() => Game1.temporaryContent.Load<Dictionary<string, string>>(reschedulerPopulate.BaseName);
}
