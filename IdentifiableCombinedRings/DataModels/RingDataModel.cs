namespace IdentifiableCombinedRings.DataModels;

/// <summary>
/// The data model used for rings. There was an idea here once.
/// </summary>
public class RingDataModel
{
    /// <summary>
    /// Either the name or int id of the rings?
    /// </summary>
    public List<string> RingIdentifiers { get; set; } = new();

    /// <summary>
    /// The path to the texture to use.
    /// </summary>
    public string TextureLocation { get; set; } = string.Empty;
}
