using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AtraBase.Toolkit.Extensions;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;

namespace HighlightEmptyMachines.Framework;
internal static class BetterBeehousesIntegration
{
    internal static MachineStatus Status { get; private set; } = MachineStatus.Disabled;

    private static IBetterBeehousesAPI? api;

    /// <summary>
    /// Tries to grab the PFM api.
    /// </summary>
    /// <param name="modRegistry">ModRegistry.</param>
    /// <returns>True if API grabbed, false otherwise.</returns>
    internal static bool TryGetAPI(IModRegistry modRegistry)
        => new IntegrationHelper(ModEntry.ModMonitor, ModEntry.TranslationHelper, modRegistry)
            .TryGetAPI("tlitookilakin.BetterBeehouses", "1.2.6", out api);

    internal static void UpdateStatus(GameLocation location)
    {
        if (location is null)
        {
            return;
        }

        if (!ModEntry.Config.VanillaMachines.SetDefault(VanillaMachinesEnum.BeeHouse, true))
        {
            Status = MachineStatus.Disabled;
            return;
        }

        if (api is null)
        {
            Status = (location.IsOutdoors && Game1.GetSeasonForLocation(location) != "winter") ? MachineStatus.Enabled : MachineStatus.Invalid;
        }
        else
        {
            Status = api.GetEnabledHere(location) ? MachineStatus.Enabled : MachineStatus.Invalid;
        }
    }
}
