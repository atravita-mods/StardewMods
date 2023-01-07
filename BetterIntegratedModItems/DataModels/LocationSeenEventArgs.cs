namespace BetterIntegratedModItems.DataModels;

public class LocationSeenEventArgs : EventArgs
{
    public LocationSeenEventArgs(string locationName) => this.LocationName = locationName;

    public string LocationName { get; init; }
}