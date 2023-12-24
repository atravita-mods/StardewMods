namespace SinZsEventTester;

#nullable enable

// Remarks:
// By default, all string fields and properties that end with "Condition" are considered possible GSQ. If you break from this naming convention
// either provide a HashSet<string> with your field names
// or a Func<string, bool> to do the filtering yourself.

/// <summary>
/// The API for this mod.
/// </summary>
public interface IEventTesterAPI
{
    /// <summary>
    /// Registers an asset to be analyzed by the GSQ checker.
    /// </summary>
    /// <param name="assetName">The asset to analyze.</param>
    /// <param name="filter">A filter to select which string fields/properties should be considered GSQ fields.</param>
    /// <returns>true if added, false otherwise.</returns>
    public bool RegisterAsset(IAssetName assetName, Func<string, bool>? filter = null);

    /// <summary>
    /// Registers an asset to be analyzed by the GSQ checker.
    /// </summary>
    /// <param name="assetName">The asset to analyze.</param>
    /// <param name="additionalGSQNames">Additional strings that may correspond to fields that should be consider GSQ fields.</param>
    /// <returns>True if added, false otherwise.</returns>
    public bool RegisterAsset(IAssetName assetName, HashSet<string> additionalGSQNames);
}
