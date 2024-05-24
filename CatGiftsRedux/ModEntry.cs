﻿// Ignore Spelling: Api

using AtraBase.Collections;
using AtraBase.Models.WeightedRandom;
using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.StringHandler;

using AtraCore.Framework.Internal;
using AtraCore.Framework.ItemManagement;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.ItemManagement;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;

using CatGiftsRedux.Framework;
using CatGiftsRedux.Framework.Pickers;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

using StardewValley.Characters;
using StardewValley.Objects;

using AtraUtils = AtraShared.Utils.Utils;
using Utils = CatGiftsRedux.Framework.Utils;

namespace CatGiftsRedux;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    private const string SAVEKEY = "GiftsThisWeek";
    private static int maxPrice;

    /// <summary>
    /// The various methods of getting an item.
    /// </summary>
    private readonly WeightedManager<Func<Random, Item?>> itemPickers = new();

    // User defined items.
    private readonly DefaultDict<ItemTypeEnum, HashSet<int>> bannedItems = new();
    private readonly WeightedManager<ItemRecord> playerItemsManager = new();
    private Lazy<WeightedManager<int>> allItemsWeighted = new(GenerateAllItems);

    private IAssetName dataObjectInfo = null!;

    private ModConfig config = null!;
    private IDynamicGameAssetsApi? dgaAPI;

    /// <summary>
    /// Gets the string utilities for this mod.
    /// </summary>
    internal static StringUtils StringUtils { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        base.Entry(helper);

        AssetManager.Initialize(helper.GameContent);
        this.config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);
        StringUtils = new(this.Monitor);
        this.dataObjectInfo = helper.GameContent.ParseAssetName("Data/ObjectInformation");

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunch;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;

        helper.Events.GameLoop.DayStarted += this.OnDayLaunched;
        helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Apply(e);
        helper.Events.Content.AssetsInvalidated += this.OnAssetInvalidated;
    }

    /// <inheritdoc />
    public override object? GetApi(IModInfo mod) => new CatGiftsReduxAPI(this, mod);

    /// <summary>
    /// Adds a possible picker.
    /// </summary>
    /// <param name="weight">The weight the picker should have.</param>
    /// <param name="picker">The picker.</param>
    internal void AddPicker(double weight, Func<Random, Item?> picker)
    {
        if (weight > 0)
        {
            this.itemPickers.Add(weight, picker);
        }
    }

    /// <inheritdoc cref="IContentEvents.AssetsInvalidated"/>
    private void OnAssetInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (this.allItemsWeighted.IsValueCreated && e.NamesWithoutLocale.Contains(this.dataObjectInfo))
        {
            maxPrice = this.config.MaxPriceForAllItems;
            this.allItemsWeighted = new(GenerateAllItems);
        }
    }

    /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
    [EventPriority(EventPriority.High)]
    private void OnDayLaunched(object? sender, DayStartedEventArgs e)
    {
        if (!Context.IsMainPlayer)
        {
            return;
        }

        if (Game1.dayOfMonth % 7 == 0 || this.Helper.Data.ReadSaveData<string>(SAVEKEY) is not string value || !int.TryParse(value, out int giftsThisWeek))
        {
            giftsThisWeek = 0;
            this.Helper.Data.WriteSaveData(SAVEKEY, "0");
        }
        else if (giftsThisWeek >= this.config.WeeklyLimit)
        {
            this.Monitor.DebugOnlyLog("Enough gifts this week");
            return;
        }

        Farm? farm = Game1.getFarm();

        if (Game1.IsRainingHere(farm) && !this.config.GiftsInRain)
        {
            return;
        }

        Pet? pet = Game1.player.getPet();
        if (pet is null)
        {
            this.Monitor.DebugOnlyLog("No pet found");
            return;
        }

        Random random = RandomUtils.GetSeededRandom(-47, "atravita.CatGiftsRedux");
        double chance = ((pet.friendshipTowardFarmer.Value / 1000.0) * (this.config.MaxChance - this.config.MinChance)) + this.config.MinChance;
        if (!random.OfChance(chance))
        {
            this.Monitor.DebugOnlyLog("Failed friendship probability check");
            return;
        }

        Vector2? tile = null;

        if (pet is Dog)
        {
            tile = farm.GetRandomTileImpl();
            if (tile is null)
            {
                this.Monitor.DebugOnlyLog("Failed to find a free tile.");
            }
        }

        // cat or fall back behavior.
        if (tile is null)
        {
            Point point = farm.GetMainFarmHouseEntry();
            tile = new(point.X, point.Y + 1);
        }

        if (this.itemPickers.Count > 0)
        {
            int attempts = 5;
            do
            {
                if (!this.itemPickers.GetValue(random).TryGetValue(out Func<Random, Item?>? picker) || picker is null)
                {
                    continue;
                }

                Item? picked = null;
                try
                {
                    picked = picker(random);
                }
                catch (Exception ex)
                {
                    this.Monitor.LogError("picking item", ex);
                    continue;
                }

                if (picked is null
                    || picked.salePrice() > this.config.MaxPriceForAllItems
                    || (this.bannedItems.TryGetValue(picked.GetItemType(), out HashSet<int>? bannedItems) && bannedItems.Contains(picked.ParentSheetIndex)))
                {
                    continue;
                }
                farm.PlaceItem(tile.Value, picked, pet);
                this.Helper.Data.WriteSaveData(SAVEKEY, (++giftsThisWeek).ToString());
                return;
            }
            while (attempts-- > 0);
        }

        this.Monitor.Log("Did not find a valid item.");
    }

    #region pickers

    private Item? GetUserItem(Random random)
    {
        if (!this.playerItemsManager.GetValue(random).TryGetValue(out ItemRecord? entry) || entry is null)
        {
            return null;
        }

        if (entry.Type.HasFlag(ItemTypeEnum.DGAItem))
        {
            if (this.dgaAPI is null)
            {
                this.Monitor.LogOnce("DGA item requested but DGA was not installed, ignored.", LogLevel.Warn);
            }

            return this.dgaAPI?.SpawnDGAItem(entry.Identifier) as Item;
        }

        if (!int.TryParse(entry.Identifier, out int id))
        {
            id = DataToItemMap.GetID(entry.Type, entry.Identifier);
        }

        if (id > 0)
        {
            return ItemUtils.GetItemFromIdentifier(entry.Type, id);
        }
        return null;
    }

    private SObject? RandomSeasonalForage(Random random)
        => new(Utility.getRandomBasicSeasonalForageItem(Game1.currentSeason, random.Next()), 1);

    private SObject? RandomSeasonalItem(Random random)
        => new(Utility.getRandomPureSeasonalItem(Game1.currentSeason, random.Next()), 1);

    private SObject? GetFromForage(Random random)
    {
        this.Monitor.DebugOnlyLog("Selected GetFromForage");

        if (this.config.ForageFromMaps.Count == 0)
        {
            return null;
        }

        string? map = Utility.GetRandom(this.config.ForageFromMaps);

        GameLocation? loc = Game1.getLocationFromName(map);
        if (loc is null)
        {
            return null;
        }

        List<SObject>? forage = loc.Objects.Values.Where((obj) => !obj.bigCraftable.Value && obj.isForage(loc)).ToList();

        if (forage.Count == 0)
        {
            return null;
        }

        return forage[random.Next(forage.Count)].getOne() as SObject;
    }

    private Item? AllItemsPicker(Random random)
    {
        this.Monitor.DebugOnlyLog("Selected all items");

        if (this.config.AllItemsWeight <= 0 || this.allItemsWeighted.Value.Count == 0)
        {
            return null;
        }
        int tries = 3;
        do
        {
            if (!this.allItemsWeighted.Value.GetValue(random).TryGetValue(out int id))
            {
                continue;
            }

            // confirm the item exists, ban Qi items or golden walnuts
            if (Utils.ForbiddenFromRandomPicking(id))
            {
                continue;
            }

            if (DataToItemMap.IsActuallyRing(id))
            {
                return new Ring(id);
            }

            return new SObject(id, 1);
        }
        while (tries-- > 0);
        return null;
    }

    #endregion

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) => this.LoadDataFromConfig();

    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    private void OnGameLaunch(object? sender, GameLaunchedEventArgs e)
    {
        IntegrationHelper integration = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, LogLevel.Trace);
        integration.TryGetAPI("spacechase0.DynamicGameAssets", "1.4.3", out this.dgaAPI);

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
            this.Helper.AsyncWriteConfig(this.Monitor, this.config);
        }

        maxPrice = this.config.MaxPriceForAllItems;

        // Handle banned items.
        this.bannedItems.Clear();
        foreach (ItemRecord? item in this.config.Denylist)
        {
            if (!int.TryParse(item.Identifier, out int id))
            {
                id = DataToItemMap.GetID(item.Type, item.Identifier);
            }

            if (id != -1)
            {
                this.bannedItems[item.Type].Add(id);
            }
        }

        // Handle the player-added list.
        this.playerItemsManager.Clear();
        if (this.config.UserDefinedItemList.Count > 0)
        {
            this.playerItemsManager.AddRange(this.config.UserDefinedItemList.Select((item) => new WeightedItem<ItemRecord?>(item.Weight, item.Item)));
        }

        // add pickers to the picking list.
        this.itemPickers.Clear();

        this.AddPicker(100, this.RandomSeasonalForage);
        this.AddPicker(100, this.RandomSeasonalItem);

        if (this.playerItemsManager.Count > 0)
        {
            this.AddPicker(this.config.UserDefinedListWeight, this.GetUserItem);
        }

        if (this.config.ForageFromMaps.Count > 0)
        {
            this.AddPicker(this.config.ForageFromMapsWeight, this.GetFromForage);
        }

        this.AddPicker(this.config.AnimalProductsWeight, AnimalProductPicker.Pick);
        this.AddPicker(this.config.SeasonalCropsWeight, SeasonalCropPicker.Pick);
        this.AddPicker(this.config.OnFarmCropWeight, OnFarmCropPicker.Pick);
        this.AddPicker(this.config.SeasonalFruitWeight, SeasonalFruitPicker.Pick);
        this.AddPicker(this.config.RingsWeight, RingPicker.Pick);
        this.AddPicker(this.config.DailyDishWeight, DailyDishPicker.Pick);
        this.AddPicker(this.config.HatWeight, HatPicker.Pick);
        this.AddPicker(this.config.ModDefinedWeight, AssetManager.Pick);

        if (this.config.AllItemsWeight > 0)
        {
            if (this.allItemsWeighted.IsValueCreated)
            {
                this.allItemsWeighted = new(GenerateAllItems);
            }
            this.AddPicker(this.config.AllItemsWeight, this.AllItemsPicker);
        }

        this.Monitor.Log($"{this.itemPickers.Count} pickers found.");
    }

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Reviewed.")]
    private static WeightedManager<int> GenerateAllItems()
    {
        WeightedManager<int> ret = new();
        float difficulty = Game1.player?.difficultyModifier ?? 1.0f;

        foreach (int key in DataToItemMap.GetAll(ItemTypeEnum.SObject))
        {
            if (Game1.objectInformation.TryGetValue(key, out string? data))
            {
                StreamSplit splits = data.StreamSplit('/');

                // field 0 - internal name.
                if (!splits.MoveNext() || splits.Current.Contains("Qi", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // field 1 - price.
                if (!splits.MoveNext() || !int.TryParse(splits.Current, out int price) || price * difficulty >= maxPrice)
                {
                    continue;
                }

                _ = splits.MoveNext();

                // field 3 - category
                if (!splits.MoveNext() || splits.Current.Word.Equals("Quest", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ret.Add(new(maxPrice - price, key));
            }
        }

        return ret;
    }
}
