using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

using StardewValley.GameData.Shops;

namespace CritterRings.Framework;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
#region asset names
    private static IAssetName dataObjectInfo = null!;
    private static IAssetName ringTextureLocation = null!;
    private static string ringTextureBackslashed = null!;
    private static IAssetName buffTextureLocation = null!;
    private static IAssetName dataShops = null!;
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
        dataObjectInfo = parser.ParseAssetName("Data/ObjectInformation");
        ringTextureLocation = parser.ParseAssetName("Mods/atravita/CritterRings/RingTex");
        ringTextureBackslashed = ringTextureLocation.BaseName.Replace('/', '\\');
        buffTextureLocation = parser.ParseAssetName("Mods/atravita/CritterRings/BuffIcon");
        dataShops = parser.ParseAssetName("Data/Shops");
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
                apply: static (asset) =>
                {
                    IDictionary<string, string> editor = asset.AsDictionary<string, string>().Data;
                    editor[ModEntry.BunnyRing] = $"atravita.BunnyRing/1000/-300/Ring/{I18n.BunnyRing_Name()}/{I18n.BunnyRing_Description()}////4/{ringTextureBackslashed}";
                    editor[ModEntry.ButterflyRing] = $"atravita.ButterflyRing/1000/-300/Ring/{I18n.ButterflyRing_Name()}/{I18n.ButterflyRing_Description()}////0/{ringTextureBackslashed}";
                    editor[ModEntry.FireFlyRing] = $"atravita.FireFlyRing/1000/-300/Ring/{I18n.FireflyRing_Name()}/{I18n.FireflyRing_Description()}////1/{ringTextureBackslashed}";
                    editor[ModEntry.FrogRing] = $"atravita.FrogRing/1000/-300/Ring/{I18n.FrogRing_Name()}/{I18n.FrogRing_Description()}////5/{ringTextureBackslashed}";
                    editor[ModEntry.OwlRing] = $"atravita.OwlRing/1000/-300/Ring/{I18n.OwlRing_Name()}/{I18n.OwlRing_Description()}////4/{ringTextureBackslashed}";
                },
                priority: AssetEditPriority.Early);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(dataShops))
        {
            e.Edit(
                apply: static (asset) =>
                {
                    if (!asset.AsDictionary<string, ShopData>().Data.TryGetValue("AdventureShop", out ShopData? shop))
                    {
                        ModEntry.ModMonitor.Log($"Failed to find AdventureShop while editing.", LogLevel.Warn);
                        return;
                    }
                    const int RING_COST = 2_500;
                    const int LATE_RING_COST = 5_000;

                    const string HasSkullKey = "atravita.AtraCore_HAS_WALLET_ITEM Current SkullKey";
                    const string HasMagicInk = $"{HasSkullKey}, atravita.AtraCore_HAS_WALLET_ITEM Current MagicInk";

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
                },
                priority: AssetEditPriority.Early);
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
}
