namespace StopRugRemoval.Framework.Niceties;

using AtraShared.Utils.Extensions;

/// <summary>
/// Fixes up animals at the end of day if weird things happen.
/// </summary>
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

        foreach (GameLocation? location in Game1.locations)
        {
            if (location is AnimalHouse || location?.animals.Count() is null or 0)
            {
                continue;
            }

            ModEntry.ModMonitor.Log($"Found {location.animals.Count()} animals outside on {location.NameOrUniqueName}", LogLevel.Info);

            foreach (FarmAnimal? animal in location.animals.Values.ToArray())
            {
                try
                {
                    animal.warpHome();
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor.LogError($"warping {animal.Name} home.", ex);
                }
            }
        }
    }
}
