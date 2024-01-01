namespace StopRugRemoval.Framework.Niceties;

using AtraShared.Utils.Extensions;

using StardewValley.Buildings;

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
                    if (animal.home is null)
                    {
                        foreach (Building? building in location.buildings)
                        {
                            if (building?.GetIndoors() is AnimalHouse house && house.animalsThatLiveHere.Contains(animal.myID.Value))
                            {
                                animal.home = building;
                                break;
                            }
                        }
                    }
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
