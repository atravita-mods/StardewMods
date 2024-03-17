using AtraCore;
using AtraCore.Framework.Models;

using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;

namespace CritterRings.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
#region asset names
    private static IAssetName dataObjectInfo = null!;
    private static IAssetName objectStrings = null!;
    private static IAssetName ringTextureLocation = null!;
    private static IAssetName buffTextureLocation = null!;
    private static IAssetName dataShops = null!;
    private static IAssetName ringData = null!;
#endregion

    private static Lazy<Texture2D> buffTex = new(() => Game1.content.Load<Texture2D>(buffTextureLocation.BaseName));

    /// <summary>
    /// Gets the location of the buff icon texture.
    /// </summary>
    internal static Texture2D BuffTexture => buffTex.Value;

    /// <summary>
    /// Initializes this asset manager.
    /// </summary>
    /// <param name="parser">Game content helper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        dataObjectInfo = parser.ParseAssetName("Data/Objects");
        objectStrings = parser.ParseAssetName("Strings/Objects");
        ringTextureLocation = parser.ParseAssetName("Mods/atravita/CritterRings/RingTex");
        buffTextureLocation = parser.ParseAssetName("Mods/atravita/CritterRings/BuffIcon");
        dataShops = parser.ParseAssetName("Data/Shops");
        ringData = parser.ParseAssetName(AtraCoreConstants.EquipData);
    }

    /// <inheritdoc cref="IContentEvents.AssetRequested"/>
    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(buffTextureLocation))
        {
            e.LoadFromModFile<Texture2D>("assets/bunnies_fast.png", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(ringTextureLocation))
        {
            e.LoadFromModFile<Texture2D>("assets/Rings.png", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(dataObjectInfo))
        {
            e.Edit(
                apply: AddRings,
                priority: AssetEditPriority.Early);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(objectStrings))
        {
            e.Edit(
                apply: AddStrings,
                priority: AssetEditPriority.Default);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(dataShops))
        {
            e.Edit(
                apply: EditShops,
                priority: AssetEditPriority.Early);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(ringData))
        {
            e.Edit(
                apply: AddRingData,
                priority: AssetEditPriority.Late);
        }
    }

    /// <inheritdoc cref="IContentEvents.AssetsInvalidated"/>
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if (buffTex.IsValueCreated && (assets is null || assets.Contains(buffTextureLocation)))
        {
            buffTex = new(static () => Game1.content.Load<Texture2D>(buffTextureLocation.BaseName));
        }
    }

    private static void AddRings(IAssetData asset)
    {
        const string RING = "Ring";
        const int RING_ID = SObject.ringCategory;
        const int PRICE = 1000;
        IDictionary<string, ObjectData> editor = asset.AsDictionary<string, ObjectData>().Data;
        editor[ModEntry.BunnyRing] = new()
        {
            Name = "atravita.BunnyRing",
            DisplayName = $"[LocalizedText Strings\\Objects:{ModEntry.BunnyRing}_Name]",
            Description = $"[LocalizedText Strings\\Objects:{ModEntry.BunnyRing}_Description]",
            Type = RING,
            Category = RING_ID,
            Price = PRICE,
            Texture = ringTextureLocation.BaseName,
            SpriteIndex = 3,
            ContextTags = ["color_gray"],
        };
        editor[ModEntry.ButterflyRing] = new()
        {
            Name = "atravita.ButterflyRing",
            DisplayName = $"[LocalizedText Strings\\Objects:{ModEntry.ButterflyRing}_Name]",
            Description = $"[LocalizedText Strings\\Objects:{ModEntry.ButterflyRing}_Description]",
            Type = RING,
            Category = RING_ID,
            Price = PRICE,
            Texture = ringTextureLocation.BaseName,
            SpriteIndex = 0,
            ContextTags = ["color_purple"],
        };
        editor[ModEntry.FireFlyRing] = new()
        {
            Name = "atravita.FireFlyRing",
            DisplayName = $"[LocalizedText Strings\\Objects:{ModEntry.FireFlyRing}_Name]",
            Description = $"[LocalizedText Strings\\Objects:{ModEntry.FireFlyRing}_Description]",
            Type = RING,
            Category = RING_ID,
            Price = PRICE,
            Texture = ringTextureLocation.BaseName,
            SpriteIndex = 1,
            ContextTags = ["color_orange"],
        };
        editor[ModEntry.FrogRing] = new()
        {
            Name = "atravita.FrogRing",
            DisplayName = $"[LocalizedText Strings\\Objects:{ModEntry.FrogRing}_Name]",
            Description = $"[LocalizedText Strings\\Objects:{ModEntry.FrogRing}_Description]",
            Type = RING,
            Category = RING_ID,
            Price = PRICE,
            Texture = ringTextureLocation.BaseName,
            SpriteIndex = 5,
            ContextTags = ["color_green"],
        };
        editor[ModEntry.OwlRing] = new()
        {
            Name = "atravita.OwlRing",
            DisplayName = $"[LocalizedText Strings\\Objects:{ModEntry.OwlRing}_Name]",
            Description = $"[LocalizedText Strings\\Objects:{ModEntry.OwlRing}_Description]",
            Type = RING,
            Category = RING_ID,
            Price = PRICE,
            Texture = ringTextureLocation.BaseName,
            SpriteIndex = 4,
            ContextTags = ["color_brown"],
        };
    }

    private static void AddRingData(IAssetData asset)
    {
        IDictionary<string, EquipmentExtModel> editor = asset.AsDictionary<string, EquipmentExtModel>().Data;
        editor[$"{ItemRegistry.type_object}{ModEntry.ButterflyRing}"] = new()
        {
            Effects =
            [
                new()
                {
                    BaseEffects = new()
                    {
                        MagneticRadius = 128,
                    },
                },
            ],
        };

        editor[$"{ItemRegistry.type_object}{ModEntry.FireFlyRing}"] = new()
        {
            Effects =
            [
                new()
                {
                    Light = new()
                    {
                        Radius = 12,
                    },
                },
            ],
        };
    }

    private static void AddStrings(IAssetData asset)
    {
        IDictionary<string, string> editor = asset.AsDictionary<string, string>().Data;
        const string NAME = "Name";
        const string DESCRIPTION = "Description";

        editor[$"{ModEntry.BunnyRing}_{NAME}"] = I18n.BunnyRing_Name();
        editor[$"{ModEntry.BunnyRing}_{DESCRIPTION}"] = I18n.BunnyRing_Description();

        editor[$"{ModEntry.ButterflyRing}_{NAME}"] = I18n.ButterflyRing_Name();
        editor[$"{ModEntry.ButterflyRing}_{DESCRIPTION}"] = I18n.ButterflyRing_Description();

        editor[$"{ModEntry.FireFlyRing}_{NAME}"] = I18n.FireflyRing_Name();
        editor[$"{ModEntry.FireFlyRing}_{DESCRIPTION}"] = I18n.FireflyRing_Description();

        editor[$"{ModEntry.FrogRing}_{NAME}"] = I18n.FrogRing_Name();
        editor[$"{ModEntry.FrogRing}_{DESCRIPTION}"] = I18n.FrogRing_Description();

        editor[$"{ModEntry.OwlRing}_{NAME}"] = I18n.OwlRing_Name();
        editor[$"{ModEntry.OwlRing}_{DESCRIPTION}"] = I18n.OwlRing_Description();
    }

    private static void EditShops(IAssetData asset)
    {
        if (!asset.AsDictionary<string, ShopData>().Data.TryGetValue("AdventureShop", out ShopData? shop))
        {
            ModEntry.ModMonitor.Log($"Failed to find AdventureShop while editing.", LogLevel.Warn);
            return;
        }
        const int RING_COST = 2_500;
        const int LATE_RING_COST = 5_000;

        const string HasSkullKey = "PLAYER_HAS_MAIL Current HasSkullKey";
        const string HasMagicInk = $"{HasSkullKey}, PLAYER_HAS_MAIL Current HasMagicInk";

        shop.Items.Add(new()
        {
            ItemId = $"{ItemRegistry.type_object}{ModEntry.BunnyRing}",
            Price = LATE_RING_COST,
            Condition = HasMagicInk,
            IgnoreShopPriceModifiers = true,
        });
        shop.Items.Add(new()
        {
            ItemId = $"{ItemRegistry.type_object}{ModEntry.ButterflyRing}",
            Price = RING_COST,
            Condition = HasSkullKey,
            IgnoreShopPriceModifiers = true,
        });
        shop.Items.Add(new()
        {
            ItemId = $"{ItemRegistry.type_object}{ModEntry.FireFlyRing}",
            Price = RING_COST,
            Condition = HasSkullKey,
            IgnoreShopPriceModifiers = true,
        });
        shop.Items.Add(new()
        {
            ItemId = $"{ItemRegistry.type_object}{ModEntry.FrogRing}",
            Price = LATE_RING_COST,
            Condition = HasMagicInk,
            IgnoreShopPriceModifiers = true,
        });
        shop.Items.Add(new()
        {
            ItemId = $"{ItemRegistry.type_object}{ModEntry.OwlRing}",
            Price = LATE_RING_COST,
            Condition = HasMagicInk,
            IgnoreShopPriceModifiers = true,
        });
    }
}
