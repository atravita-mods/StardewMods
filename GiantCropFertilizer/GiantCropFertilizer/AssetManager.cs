using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

using StardewValley.GameData.Objects;
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
    #endregion

    /// <summary>
    /// Initializes this asset manager.
    /// </summary>
    /// <param name="parser">Game content helper.</param>
    internal static void Init(IGameContentHelper parser)
    {
        dataObjectInfo = parser.ParseAssetName("Data/Objects");
        dataShops = parser.ParseAssetName("Data/Shops");
        textureLocation = parser.ParseAssetName("Mods/atravita/GiantCropFertilizer/Object");
    }

    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (ModEntry.Config.AllowGiantCropsOffFarm && e.DataType == typeof(xTile.Map))
        {
            e.Edit(static asset =>
            {
                asset.AsMap().Data.Properties["AllowGiantCrops"] = new("T");
            });
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(textureLocation))
        {
            e.LoadFromModFile<Texture2D>("assets/object.png", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(dataObjectInfo))
        {
            e.Edit(
                apply: static (asset) =>
                {
                    asset.AsDictionary<string, ObjectData>().Data[ModEntry.GiantCropFertilizerID] = new()
                    {
                        Name = "Giant Crop Fertilizer",
                        Price = 100,
                        Edibility = -300,
                        Category = SObject.fertilizerCategory,
                        Type = "Basic",
                        DisplayName = I18n.GiantCropFertilizer_Name(),
                        Description = I18n.GiantCropFertilizer_Description(),
                        Texture = textureLocation.BaseName,
                        SpriteIndex = 0,
                    };
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
                    TradeItemId = "(O)858",
                    TradeItemAmount = 5,
                });
            });
        }
    }
}
