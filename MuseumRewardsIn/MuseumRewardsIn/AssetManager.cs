using AtraShared.Utils.Extensions;

using StardewModdingAPI.Events;

using StardewValley.GameData;
using StardewValley.GameData.Shops;

namespace MuseumRewardsIn;

/// <summary>
/// Manages assets for this mod.
/// </summary>
internal static class AssetManager
{
    private const string BUILDING = "Buildings";
    private const string SHOPNAME = "atravita.MuseumShop";

    private static Lazy<HashSet<string>> mailflags = new(GetMailFlagsForStore);

    private static IAssetName letters = null!;
    private static IAssetName shops = null!;
    private static IAssetName strings = null!;

    /// <summary>
    /// Gets the asset name for the library.
    /// </summary>
    internal static IAssetName LibraryHouse { get; private set; } = null!;

    /// <summary>
    /// Gets a hashset of mailflags to process for gifts.
    /// </summary>
    internal static HashSet<string> MailFlags => mailflags.Value;

    /// <summary>
    /// Initializes the AssetManager.
    /// </summary>
    /// <param name="parser">Game Content Helper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        letters = parser.ParseAssetName("Mods/atravita/MuseumStore/Letters");
        shops = parser.ParseAssetName("Data/Shops");
        LibraryHouse = parser.ParseAssetName("Maps/ArchaeologyHouse");
        strings = parser.ParseAssetName("Mods/atravita/MuseumStore/Strings");
    }

    /// <summary>
    /// Listens for invalidations to drop the cache if needed.
    /// </summary>
    /// <param name="names">Hashset of assetnames invalidated.</param>
    internal static void Invalidate(IReadOnlySet<IAssetName>? names = null)
    {
        if (mailflags.IsValueCreated && (names is null || names.Contains(letters)))
        {
            mailflags = new(GetMailFlagsForStore);
        }
    }

    /// <summary>
    /// Applies the asset loads.
    /// </summary>
    /// <param name="e">Event args.</param>
    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(letters))
        {
            e.LoadFromModFile<Dictionary<string, string>>("assets/vanilla_mail.json", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(LibraryHouse))
        {
            e.Edit(
                apply: static (asset) => asset.AsMap().AddTileProperty(
                    monitor: ModEntry.ModMonitor,
                    layer: BUILDING,
                    key: "Action",
                    property: $"OpenShop {SHOPNAME} down",
                    placementTile: ModEntry.Config.BoxLocation),
                priority: AssetEditPriority.Default + 10);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(shops))
        {
            e.Edit(AddShops);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(strings))
        {
            e.LoadFrom(
                load: () => new Dictionary<string, string>
                {
                    ["GuntherDialogue"] = I18n.ShopMessage(),
                },
                priority: AssetLoadPriority.Exclusive
            );
        }
    }

    private static void AddShops(IAssetData data)
    {
        IDictionary<string, ShopData> editor = data.AsDictionary<string, ShopData>().Data;

        ShopData museumShop = new()
        {
            Owners =
            [
                new()
                {
                    Name = nameof(ShopOwnerType.AnyOrNone),
                    Portrait = "Portraits/Gunther",
                    Dialogues =
                    [
                        new()
                        {
                            Id = "Default",
                            Dialogue = $"[LocalizedText {strings.BaseName}:GuntherDialogue]",
                        },
                    ],
                },
            ],
            Items =
            [
                new()
                {
                    PriceModifierMode = QuantityModifier.QuantityModifierMode.Maximum,
                    PriceModifiers =
                    [
                        new()
                        {
                            Modification = QuantityModifier.ModificationType.Multiply,
                            Amount = 3,
                        },
                        new()
                        {
                            Modification = QuantityModifier.ModificationType.Set,
                            Amount = 2000,
                        },
                    ],
                    ItemId = ModEntry.MUSEUM_RESOLVER,
                },
            ],
            ApplyProfitMargins = false,
        };

        if (ModEntry.Config.AllowBuyBacks)
        {
            museumShop.SalableItemTags =
            [
                "category_gem",
                "category_minerals",
                "item_type_arch",
            ];
        }
        editor[SHOPNAME] = museumShop;

        if (!editor.TryGetValue("Furniture Catalogue", out ShopData? furniture))
        {
            ModEntry.ModMonitor.Log($"Failed to get furniture catalogue for editing", LogLevel.Warn);
            return;
        }

        furniture.Items.Add(new()
        {
            ItemId = $"{ModEntry.MUSEUM_RESOLVER} @has_type (F)",
            Price = 0,
        });
    }

    private static HashSet<string> GetMailFlagsForStore()
        => Game1.temporaryContent.Load<Dictionary<string, string>>(letters.BaseName).Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
}
