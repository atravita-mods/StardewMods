using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AtraShared.Utils.Extensions;

namespace StopRugRemoval.Framework.Niceties;
internal static class AnimalSanity
{
    /// <summary>
    /// Fixes up animals.
    /// </summary>
    internal static void FixAnimals()
    {
        if (Context.IsMainPlayer)
        {
            return;
        }

        foreach (var location in Game1.locations)
        {
            if (location is AnimalHouse || location?.animals.Count() is null or 0)
            {
                continue;
            }

            ModEntry.ModMonitor.Log($"Found {location.animals.Count()} animals outside on {location.NameOrUniqueName}", LogLevel.Info);

            foreach (var animal in location.animals.Values.ToArray())
            {
                try
                {
                    animal.warpHome(location, animal);
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor.LogError($"warping {animal.Name} home.", ex);
                }
            }
        }
    }
}
