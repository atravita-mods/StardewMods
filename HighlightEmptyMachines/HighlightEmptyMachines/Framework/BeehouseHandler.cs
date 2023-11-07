using System.Runtime.CompilerServices;

using AtraBase.Toolkit.Extensions;

using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.Utils.Extensions;

using StardewModdingAPI.Utilities;

namespace HighlightEmptyMachines.Framework;

/// <summary>
/// Handles the beehives.
/// </summary>
internal static class BeehouseHandler
{
    internal const string BeeHouse = "(BC)10";

    private static IBetterBeehousesAPI? api;

    /// <summary>
    /// Gets the current beehouse status.
    /// </summary>
    internal static PerScreen<MachineStatus> Status { get; } = new(() => MachineStatus.Disabled);

    /// <summary>
    /// Tries to grab the PFM api.
    /// </summary>
    /// <param name="modRegistry">ModRegistry.</param>
    /// <returns>True if API grabbed, false otherwise.</returns>
    internal static bool TryGetAPI(IModRegistry modRegistry)
        => new IntegrationHelper(ModEntry.ModMonitor, ModEntry.TranslationHelper, modRegistry)
            .TryGetAPI("tlitookilakin.BetterBeehouses", "1.2.6", out api);

    /// <summary>
    /// Updates the status of beehives for the current location.
    /// </summary>
    /// <param name="location">The game location to update to.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal static void UpdateStatus(GameLocation? location)
    {
        if (location is null)
        {
            return;
        }

        if (!ModEntry.Config.VanillaMachines.SetDefault("(BC)10", true))
        {
            Status.Value = MachineStatus.Disabled;
            return;
        }

        if (api is null)
        {
            Status.Value = (location.IsOutdoors && location.GetSeason() == Season.Winter) ? MachineStatus.Enabled : MachineStatus.Invalid;
        }
        else
        {
            Status.Value = api.GetEnabledHere(location, location.GetSeason() == Season.Winter) ? MachineStatus.Enabled : MachineStatus.Invalid;
        }

        ModEntry.ModMonitor.DebugOnlyLog($"Current status of beehives is {Status.Value}");
    }
}
