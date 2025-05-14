namespace NovaNPCTest;

using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;

public class ChaosTotemData
{
    public string Location { get; set; }

    public Vector2 Position { get; set; }

    public string Color { get; set; }
}

internal class ChaosTotemAsset
{
    private static Dictionary<string, ChaosTotemData>? asset = null;

    internal static Dictionary<string, ChaosTotemData> Asset
    {
        get 
        {
            return asset ??= Game1.content.Load<Dictionary<string, ChaosTotemData>>("TenebrousNova.EnD/ChaosTotemLocations");
        }
    }

    public static void AssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("TenebrousNova.EnD/ChaosTotemLocations"))
        {
            e.LoadFrom(() => new Dictionary<string, ChaosTotemData>(), AssetLoadPriority.Exclusive);
        }
    }

    public static void AssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (e.NamesWithoutLocale.Any(asset => asset.IsEquivalentTo("TenebrousNova.EnD/ChaosTotemLocations")))
        {
            asset = null;
        }
    }
}
