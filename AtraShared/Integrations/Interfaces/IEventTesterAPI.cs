namespace AtraShared.Integrations.Interfaces;

public interface IEventTesterAPI
{
    public bool RegisterAsset(IAssetName assetName, Func<string, bool>? filter = null);

    public bool RegisterAsset(IAssetName assetName, HashSet<string> additionalGSQNames);
}
