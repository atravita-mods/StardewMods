using SlightlyMoreDehardcoding.Models;

using StardewModdingAPI.Events;

namespace SlightlyMoreDehardcoding.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private static IAssetName mailAsset = null!;
    private static IAssetName friendshipAsset = null!;

    private static Dictionary<string, ExtendedFriendshipData>? _friendship = null;

    internal static void Init(IGameContentHelper parser, string uniqueID)
    {
        mailAsset = parser.ParseAssetName($"Mods/{uniqueID}/mail");
        friendshipAsset = parser.ParseAssetName($"Mods/{uniqueID}/friendship");
    }

    internal static void Apply(AssetRequestedEventArgs args)
    {
        if (args.NameWithoutLocale.Equals(mailAsset))
        {
            args.LoadFrom(static () => new Dictionary<string, ExtendedMailData>(), AssetLoadPriority.Exclusive);
        }
        if (args.NameWithoutLocale.Equals(friendshipAsset))
        {
            args.LoadFrom(static () => new Dictionary<string, ExtendedFriendshipData>(), AssetLoadPriority.Exclusive);
        }

    }

    internal static void Invalidate(IReadOnlySet<IAssetName>? assets)
    {
        if (assets is null || assets.Contains(friendshipAsset))
        {
            _friendship = null!;
        }
    }

    internal static ExtendedFriendshipData? GetExtendedFriendshipData(string npcName)
    {
        _friendship ??= Game1.content.Load<Dictionary<string, ExtendedFriendshipData>>(friendshipAsset.BaseName);
        return _friendship.GetValueOrDefault(npcName);
    }

    internal static ExtendedMailData? GetExtendedMailData(string mailkey)
        => Game1.content.Load<Dictionary<string,  ExtendedMailData>>(mailAsset.BaseName).GetValueOrDefault(mailkey);
}
