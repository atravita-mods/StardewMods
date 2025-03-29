using SlightlyMoreDehardcoding.Models;

using StardewModdingAPI.Events;

namespace SlightlyMoreDehardcoding.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static IAssetName mailAsset = null!;

    internal static void Init(IGameContentHelper parser, string uniqueID)
    {
        mailAsset = parser.ParseAssetName($"Mods/{uniqueID}/mailAsset");
    }

    internal static void Apply(AssetRequestedEventArgs args)
    {
        if (args.NameWithoutLocale.Equals(mailAsset))
        {
            args.LoadFrom(static () => new Dictionary<string, ExtendedMailData>(), AssetLoadPriority.Exclusive);
        }
    }

    internal static ExtendedMailData? GetExtendedMailData(string mailkey)
        => Game1.content.Load<Dictionary<string,  ExtendedMailData>>(mailAsset.Name).GetValueOrDefault(mailkey);
}
