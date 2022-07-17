namespace IdentifiableCombinedRings.DataModels;

/// <summary>
/// The data model used for rings. There was an idea here once.
/// </summary>
public class RingDataModel
{
    public List<string> RingIdentifiers { get; set; } = new();

    public string TextureLocation { get; set; } = string.Empty;
}
