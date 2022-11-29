namespace BetterIntegratedModItems.Framework.DataModels;

public sealed class LocationWatcher
{
    public HashSet<string> SeenLocations { get; set; } = new();
}