// Ignore Spelling: Api

namespace SinZsEventTester.Framework;

/// <inheritdoc cref="IEventTesterAPI"/>
public sealed class Api(IModInfo other, IMonitor monitor) : IEventTesterAPI
{
    /// <inheritdoc />
    public bool RegisterAsset(IAssetName assetName, Func<string, bool>? filter)
    {
        monitor.Log($"{other.Manifest.UniqueID} is registering {assetName.BaseName}");
        return GSQTester.Register(assetName, filter);
    }

    /// <inheritdoc />
    public bool RegisterAsset(IAssetName assetName, HashSet<string> additionalGSQNames)
    {
        monitor.Log($"{other.Manifest.UniqueID} is registering {assetName.BaseName}");
        return GSQTester.Register(assetName, additionalGSQNames);
    }

    /// <inheritdoc />
    public bool RemoveAsset(IAssetName assetName)
    {
        monitor.Log($"{other.Manifest.UniqueID} is removing {assetName.BaseName}");
        return GSQTester.Remove(assetName);
    }
}
