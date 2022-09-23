namespace IdentifiableCombinedRings.DataModels;

/// <summary>
/// The data model used for rings. There was an idea here once.
/// </summary>
public sealed class RingDataModel
{
    /// <summary>
    /// Gets or sets either the name or int id of the rings.
    /// </summary>
    public HashSet<string> RingIdentifiers { get; set; } = new();

    /// <summary>
    /// Gets or sets the path to the texture to use.
    /// </summary>
    public string TextureLocation { get; set; } = string.Empty;
}
