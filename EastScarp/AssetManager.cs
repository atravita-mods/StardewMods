using EastScarp.Models;

using StardewModdingAPI.Events;

namespace EastScarp;

internal static class AssetManager
{
    private static IAssetName locationExtendedModelLocation = null!;
    private static Lazy<Dictionary<string, Model>> _data = new(() => Game1.content.Load<Dictionary<string, Model>>(locationExtendedModelLocation.BaseName));

    internal static void Init(IGameContentHelper parser)
    {
        locationExtendedModelLocation = parser.ParseAssetName("Mods/EastScarp/LocationMetadata");
    }

    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(locationExtendedModelLocation))
        {
            e.LoadFromModFile<Dictionary<string, Model>>("assets/data.json", AssetLoadPriority.Exclusive);
        }
    }

    internal static void Invalidate(IReadOnlySet<IAssetName>? assets)
    {
        if (assets is null || assets.Contains(locationExtendedModelLocation))
        {
            _data = new(() => Game1.content.Load<Dictionary<string, Model>>(locationExtendedModelLocation.BaseName));
        }
    }
}
