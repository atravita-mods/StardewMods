using AtraBase.Models.WeightedRandom;
using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.ItemManagement;
using AtraCore.Framework.QueuePlayerAlert;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.ItemManagement;
using AtraShared.Utils.Extensions;

using CatGiftsRedux.Framework;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

using StardewValley.Characters;
using StardewValley.Objects;

using AtraUtils = AtraShared.Utils.Utils;

namespace CatGiftsRedux;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private const string SAVEKEY = "GiftsThisWeek";

    private readonly HashSet<int> bannedItems = new();
    private readonly WeightedManager<ItemRecord> playerItemsManager = new();

    private readonly WeightedManager<int> allItemsWeighted = new();

    /// <summary>
    /// The various methods of getting an item.
    /// </summary>
    private readonly List<Func<Random, SObject?>> itemPickers = new();

    private ModConfig config = null!;

    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        this.config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);
        ModMonitor = this.Monitor;

        helper.Events.GameLoop.GameLaunched += this.SetUpConfig;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;

        helper.Events.GameLoop.DayStarted += this.OnDayLaunched;
    }

    private void OnDayLaunched(object? sender, DayStartedEventArgs e)
    {
        if (this.Helper.Data.ReadSaveData<string>(SAVEKEY) is string value && int.TryParse(value, out var giftsThisWeek))
        {
            if (Game1.dayOfMonth % 7 == 0)
            {
                giftsThisWeek = 0;
            }
            else if (giftsThisWeek >= this.config.WeeklyLimit)
            {
                return;
            }
        }
        else
        {
            giftsThisWeek = 0;
        }

        Pet? pet = Game1.player.getPet();
        if (pet is null)
        {
            return;
        }

        Random random = new((int)Game1.uniqueIDForThisGame - (47 * (int)Game1.stats.daysPlayed));
        double chance = ((pet.friendshipTowardFarmer.Value / 1000.0) * (this.config.MaxChance - this.config.MinChance)) + this.config.MinChance;
        if (random.NextDouble() > chance)
        {
            return;
        }

        Farm? farm = Game1.getFarm();

        Vector2? tile;

        if (pet is Cat)
        {
            var point = farm.GetMainFarmHouseEntry();
            tile = new(point.X, point.Y + 2);
        }
        else
        {
            tile = this.GetRandomTile(farm);
        }

        if (tile is null)
        {
            return;
        }

        if (this.playerItemsManager.Count > 0 && random.NextDouble() < 0.25)
        {
            var entry = this.playerItemsManager.GetValue(random);
            if (!int.TryParse(entry.Identifier, out var id))
            {
                id = DataToItemMap.GetID(entry.Type, entry.Identifier);
            }

            if (id != -1)
            {
                var item = ItemUtils.GetItemFromIdentifier(entry.Type, id);
                if (item is not null)
                {
                    this.PlaceItem(farm, tile.Value, item, pet);
                    goto SUCCESS;
                }
            }
        }

        // tiny chance of a ring.
        if (this.config.RingsEnabled && random.NextDouble() < 0.02)
        {
            Ring? ring = RingPicker.Pick(random);
            if (ring is not null)
            {
                this.PlaceItem(farm, tile.Value, ring, pet);
                goto SUCCESS;
            }
        }

        if (this.itemPickers.Count > 0)
        {
            int attempts = 10;
            do
            {
                var picker = Utility.GetRandom(this.itemPickers, random);
                Item? picked = picker(random);

                if (picked is not null)
                {
                    if ((picked is SObject && this.bannedItems.Contains(picked.ParentSheetIndex))
                        || picked.salePrice() > this.config.MaxPriceForAllItems)
                    {
                        continue;
                    }
                    this.PlaceItem(farm, tile.Value, picked, pet);
                    goto SUCCESS;
                }
            }
            while (attempts-- > 0);
        }
        return;
SUCCESS:
        this.Helper.Data.WriteSaveData(SAVEKEY, (++giftsThisWeek).ToString());
    }

    /// <summary>
    /// Gets a random empty tile on a map.
    /// </summary>
    /// <param name="location">The game location to get a random tile from.</param>
    /// <param name="tries">How many times to try.</param>
    /// <returns>Empty tile, or null to indicate failure.</returns>
    private Vector2? GetRandomTile(GameLocation location, int tries = 10)
    {
        do
        {
            var tile = location.getRandomTile();
            if (location.isWaterTile((int)tile.X, (int)tile.Y))
            {
                continue;
            }

            var options = Utility.recursiveFindOpenTiles(location, tile, 1);
            if (options.Count > 0)
            {
                return options[0];
            }

        } while (tries-- > 0);

        return null;
    }

    private void PlaceItem(GameLocation location, Vector2 tile, Item item, Pet pet)
    {
        this.Monitor.DebugOnlyLog($"Placing {item.Name} at {location.NameOrUniqueName} - {tile}");

        PlayerAlertHandler.AddMessage(new ($"{pet.Name} has brought you a {item.DisplayName}", 1, true, Color.PaleGreen, item));

        if (item is SObject obj && !location.Objects.ContainsKey(tile))
        {
            location.Objects[tile] = obj;
        }
        else
        {
            var debris = new Debris(item, tile * 64f);
            location.debris.Add(debris);
        }
    }

    private SObject? RandomSeasonalForage(Random random)
        => new SObject(Utility.getRandomBasicSeasonalForageItem(Game1.currentSeason), 1);

    private SObject? GetFromForage(Random random)
    {
        this.Monitor.DebugOnlyLog("Selected GetFromForage");

        if (this.config.ForageFromMaps.Count == 0)
        {
            return null;
        }

        string? map = Utility.GetRandom(this.config.ForageFromMaps);

        var loc = Game1.getLocationFromName(map);
        if (loc is null)
        {
            return null;
        }

        foreach (SObject? value in loc.Objects.Values)
        {
            if (value.isForage(loc))
            {
                return value.getOne() as SObject;
            }
        }
        return null;
    }

    private SObject? AllItemsPicker(Random random)
    {
        this.Monitor.DebugOnlyLog("Selected all items");

        if (!this.config.AllItemsEnabled || this.allItemsWeighted.Count == 0)
        {
            return null;
        }

        int id = this.allItemsWeighted.GetValue(random);
        if (Game1.objectInformation.ContainsKey(id))
        {
            return new SObject(id, 1);
        }

        return null;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) => this.LoadDataFromConfig();

    /// <summary>
    /// Sets up the GMCM for this mod.
    /// </summary>
    /// <param name="sender">SMAPI.</param>
    /// <param name="e">event args.</param>
    private void SetUpConfig(object? sender, GameLaunchedEventArgs e)
    {
        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (helper.TryGetAPI())
        {
            helper.Register(
                reset: () => this.config = new(),
                save: () =>
                {
                    if (Context.IsWorldReady)
                    {
                        this.LoadDataFromConfig();
                    }
                    this.Helper.AsyncWriteConfig(this.Monitor, this.config);
                })
            .GenerateDefaultGMCM(() => this.config);
        }
    }

    private void LoadDataFromConfig()
    {
        if (this.config.MinChance > this.config.MaxChance)
        {
            // sanity check the chances.
            (this.config.MinChance, this.config.MaxChance) = (this.config.MaxChance, this.config.MinChance);
        }

        // Handle banned items.
        this.bannedItems.Clear();
        foreach (var item in this.config.Denylist)
        {
            if (item.Type != AtraShared.ConstantsAndEnums.ItemTypeEnum.SObject)
            {
                continue;
            }

            if (!int.TryParse(item.Identifier, out var id))
            {
                id = DataToItemMap.GetID(item.Type, item.Identifier);
            }

            if (id != -1)
            {
                this.bannedItems.Add(id);
            }
        }

        // Handle the player-added list.
        this.playerItemsManager.Clear();
        if (this.config.UserDefinedItemList.Count > 0)
        {
            this.playerItemsManager.AddRange(this.config.UserDefinedItemList.Select((item) => new WeightedItem<ItemRecord>(item.Weight, item.Item)));
        }

        this.itemPickers.Clear();

        this.itemPickers.Add(this.RandomSeasonalForage);

        // add pickers to the picking list.
        if (this.config.ForageFromMaps.Count > 0)
        {
            this.itemPickers.Add(this.GetFromForage);
        }

        if (this.config.AnimalProductsEnabled)
        {
            this.itemPickers.Add(AnimalProductChooser.Pick);
        }

        if (this.config.SeasonalCropsEnabled)
        {
            this.itemPickers.Add(SeasonalCropChooser.Pick);
        }

        if (this.config.OnFarmCropEnabled)
        {
            this.itemPickers.Add(OnFarmCropPicker.Pick);
        }

        if (this.config.SeasonalFruit)
        {
            this.itemPickers.Add(SeasonalFruitPicker.Pick);
        }

        this.allItemsWeighted.Clear();
        if (this.config.AllItemsEnabled)
        {
            foreach (var key in DataToItemMap.GetAll(ItemTypeEnum.SObject))
            {
                if (Game1.objectInformation.TryGetValue(key, out string? data)
                    && int.TryParse(data.GetNthChunk('/', SObject.objectInfoPriceIndex), out int price)
                    && price < this.config.MaxPriceForAllItems)
                {
                    this.allItemsWeighted.Add(new(price, key));
                }
            }
            this.itemPickers.Add(this.AllItemsPicker);
        }

        this.Monitor.Log($"{this.itemPickers.Count} pickers found.");
    }
}
