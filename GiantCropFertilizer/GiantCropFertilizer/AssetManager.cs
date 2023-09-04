using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

using StardewValley.GameData.Shops;

namespace GiantCropFertilizer;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    #region asset names
    private static IAssetName dataObjectInfo = null!;
    private static IAssetName dataShops = null!;
    private static IAssetName textureLocation = null!;
    private static string textureLocationBackslashed = null!;
    #endregion

    /// <summary>
    /// Initializes this asset manager.
    /// </summary>
    /// <param name="parser">Game content helper.</param>
    internal static void Init(IGameContentHelper parser)
    {
        dataObjectInfo = parser.ParseAssetName("Data/ObjectInformation");
        dataShops = parser.ParseAssetName("Data/Shops");
        textureLocation = parser.ParseAssetName("Mods/atravita/GiantCropFertilizer/Object");
        textureLocationBackslashed = textureLocation.BaseName.Replace('/', '\\');
    }

    ///<inheritdoc cref="IContentEvents.AssetRequested"/>
    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(textureLocation))
        {
            e.LoadFromModFile<Texture2D>("assets/object.png", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(dataObjectInfo))
        {
            e.Edit(
                apply: static (asset) =>
                {
                    asset.AsDictionary<string, string>().Data[ModEntry.GiantCropFertilizerID] = $"Giant Crop Fertilizer/100/-300/Basic -19/{I18n.GiantCropFertilizer_Name()}{I18n.GiantCropFertilizer_Description()}////0/{textureLocationBackslashed}";
                },
                priority: AssetEditPriority.Early);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(dataShops))
        {
            e.Edit(static (asset) =>
            {
                if (!asset.AsDictionary<string, ShopData>().Data.TryGetValue("QiGemShop", out ShopData? shop))
                {
                    ModEntry.ModMonitor.Log($"Could not find Qi Gen shop to edit.", LogLevel.Warn);
                    return;
                }

                shop.Items.Add(new()
                {
                    ItemId = $"{ItemRegistry.type_object}{ModEntry.GiantCropFertilizerID}",
                    TradeItemId = "O(858)",
                    TradeItemAmount = 5,
                });
            });
        }
    }
}
