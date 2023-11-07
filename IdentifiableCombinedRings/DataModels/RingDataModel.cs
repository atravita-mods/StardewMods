namespace IdentifiableCombinedRings.DataModels;

internal readonly record struct RingPair(string first, string second);

/// <summary>
/// The data model used for rings.
/// </summary>
public sealed class RingDataModel
{
    /// <summary>
    /// Gets or sets either the name or int id of the rings.
    /// </summary>
    public string? RingIdentifiers { get; set; }

    /// <summary>
    /// Gets or sets the path to the texture to use.
    /// </summary>
    public string TextureLocation { get; set; } = string.Empty;
}
