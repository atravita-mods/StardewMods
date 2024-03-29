using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

using AtraBase.Models.RentedArrayHelpers;
using AtraBase.Toolkit;
using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.Internal;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.MigrationManager;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;
using AtraShared.Wrappers;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

using StardewValley.Extensions;
using StardewValley.GameData.FruitTrees;
using StardewValley.Internal;
using StardewValley.Locations;
using StardewValley.TokenizableStrings;

using AtraUtils = AtraShared.Utils.Utils;
using XLocation = xTile.Dimensions.Location;

namespace FarmCaveSpawn;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <summary>
    /// Sublocation-parsing regex.
    /// </summary>
    private static readonly Regex Regex = new(
        // ":[(x1;y1);(x2;y2)]"
        pattern: @":\[\((?<x1>[0-9]+);(?<y1>[0-9]+)\);\((?<x2>[0-9]+);(?<y2>[0-9]+)\)\]$",
        options: RegexOptions.CultureInvariant | RegexOptions.Compiled,
        matchTimeout: TimeSpan.FromMilliseconds(250));

    private static bool ShouldResetFruitList = true;

    /// <summary>
    /// The item IDs for the four basic forage fruit.
    /// </summary>
    private readonly List<string> BASE_FRUIT = ["(O)296", "(O)396", "(O)406", "(O)410"];

    /// <summary>
    /// A list of vanilla fruit.
    /// </summary>
    private readonly List<string> VANILLA_FRUIT = ["(O)613", "(O)634", "(O)635", "(O)636", "(O)637", "(O)638"];

    /// <summary>
    /// Item IDs for items produced by trees.
    /// </summary>
    private List<string> TreeFruit = [];

    private StardewSeasons season = StardewSeasons.None;

    private MigrationManager? migrator;

    private ModConfig config = null!;

    /// <summary>
    /// Location to temporarily store the seeded random.
    /// </summary>
    private Random? random;

    /// <summary>
    /// Gets the seeded random for this mod.
    /// </summary>
    private Random Random
        => this.random ??= RandomUtils.GetSeededRandom(7, "atravita.FarmCaveSpawn.CaveRandom");

    /// <summary>
    /// Gets or sets a value indicating whether or not I've spawned fruit today.
    /// </summary>
    private bool SpawnedFruitToday { get; set; }

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        AssetManager.Initialize(helper.GameContent);
        base.Entry(helper);

        this.config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.Events.GameLoop.DayStarted += this.SpawnFruit;
        helper.Events.GameLoop.GameLaunched += this.SetUpConfig;
        helper.Events.GameLoop.OneSecondUpdateTicking += this.BellsAndWhistles;
        helper.Events.GameLoop.SaveLoaded += this.SaveLoaded;

        helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Load(e);

        // inventory watching
        InventoryWatcher.Initialize(this.ModManifest.UniqueID);
        helper.Events.GameLoop.SaveLoaded += (_, _) => InventoryWatcher.Load(helper.Multiplayer, helper.Data);
        helper.Events.Player.InventoryChanged += (_, e) => InventoryWatcher.Watch(e, helper.Multiplayer);
        helper.Events.Multiplayer.PeerConnected += (_, e) => InventoryWatcher.OnPeerConnected(e, helper.Multiplayer);
        helper.Events.Multiplayer.ModMessageReceived += static (_, e) => InventoryWatcher.OnModMessageRecieved(e);
        helper.Events.GameLoop.Saving += (_, _) => InventoryWatcher.Saving(helper.Data);

        helper.ConsoleCommands.Add(
            name: "av.fcs.list_fruits",
            documentation: I18n.ListFruits_Description(),
            callback: this.ListFruits);
    }

    /// <summary>
    /// Request the fruit list be reset the next time it's used.
    /// </summary>
    internal static void RequestFruitListReset() => ShouldResetFruitList = true;

    /// <summary>
    /// Deletes the Random as well.
    /// </summary>
    private void ResetRandom() => this.random = null;

    /// <summary>
    /// Generates the GMCM for this mod by looking at the structure of the config class.
    /// </summary>
    /// <param name="sender">Unknown, expected by SMAPI.</param>
    /// <param name="e">event args.</param>
    private void SetUpConfig(object? sender, GameLaunchedEventArgs e)
    {
        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (helper.TryGetAPI())
        {
            helper.Register(
                reset: () => this.config = new ModConfig(),
                save: () =>
                {
                    this.Helper.AsyncWriteConfig(this.Monitor, this.config);
                    ShouldResetFruitList = true;
                })
            .AddParagraph(I18n.Mod_Description)
            .GenerateDefaultGMCM(() => this.config);
        }
    }

    /// <summary>
    /// Whether or not I should spawn fruit (according to config + game state).
    /// </summary>
    /// <returns>True if I should spawn fruit, false otherwise.</returns>
    private bool ShouldSpawnFruit()
    {
        // Compat for Farm Cave Framework: https://www.nexusmods.com/stardewvalley/mods/10506
        // Which saves the farm cave choice to their own SaveData, and doesn't update the MasterPlayer.caveChoice
        bool hasFCFbatcave = false;
        if (Game1.CustomData.TryGetValue("smapi/mod-data/aedenthorn.farmcaveframework/farm-cave-framework-choice", out string? farmcavechoice))
        {
            // Crosscheck this = probably better to just use the actual value, maybe...
            hasFCFbatcave = (farmcavechoice is not null) && (farmcavechoice.Contains("bat", StringComparison.OrdinalIgnoreCase) || farmcavechoice.Contains("fruit", StringComparison.OrdinalIgnoreCase));
            this.Monitor.DebugOnlyLog(hasFCFbatcave ? "FarmCaveFramework fruit bat cave detected." : "FarmCaveFramework fruit bat cave not detected.");
        }

        if (!this.config.EarlyFarmCave
            && (Game1.MasterPlayer.caveChoice?.Value is null || Game1.MasterPlayer.caveChoice.Value <= Farmer.caveNothing)
            && string.IsNullOrWhiteSpace(farmcavechoice))
        {
            this.Monitor.DebugOnlyLog("Demetrius cutscene not seen and config not set to early, skip spawning for today.");
            return false;
        }
        if (!this.config.IgnoreFarmCaveType && !this.config.EarlyFarmCave
            && (Game1.MasterPlayer.caveChoice?.Value is null || Game1.MasterPlayer.caveChoice.Value != Farmer.caveBats)
            && !hasFCFbatcave)
        {
            this.Monitor.DebugOnlyLog("Fruit bat cave not selected and config not set to ignore that, skip spawning for today.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Handle spawning fruit at the start of each day.
    /// </summary>
    /// <param name="sender">Unknown, unused.</param>
    /// <param name="e">Arguments.</param>
    private void SpawnFruit(object? sender, DayStartedEventArgs e)
    {
        this.SpawnedFruitToday = this.ShouldSpawnFruit();

        if (!this.SpawnedFruitToday || !Context.IsMainPlayer)
        {
            return;
        }

        int count = 0;

        StardewSeasons currentSeason = StardewSeasonsExtensions.TryParse(Game1.currentSeason, value: out StardewSeasons val, ignoreCase: true) ? val : StardewSeasons.All;
        if (ShouldResetFruitList || this.season != currentSeason)
        {
            this.TreeFruit = this.GetTreeFruits();
        }
        this.season = currentSeason;
        ShouldResetFruitList = false;

        if (Game1.getLocationFromName("FarmCave") is FarmCave farmcave)
        {
            this.Monitor.DebugOnlyLog($"Spawning in the farm cave");

            (Vector2[] tiles, int num) = farmcave.GetTiles();

            if (num > 0)
            {
                foreach (Vector2 tile in new Span<Vector2>(tiles).Shuffled(num, this.Random))
                {
                    if (this.CanSpawnFruitHere(farmcave, tile))
                    {
                        this.PlaceFruit(farmcave, tile);
                        if (++count >= this.config.MaxDailySpawns)
                        {
                            break;
                        }
                    }
                }

                ArrayPool<Vector2>.Shared.Return(tiles);
                farmcave.UpdateReadyFlag();
            }

            if (count >= this.config.MaxDailySpawns)
            {
                goto END;
            }
        }

        if (this.config.UseModCaves)
        {
            foreach (string location in this.GetData(AssetManager.ADDITIONAL_LOCATIONS_LOCATION))
            {
                string parseloc = location;
                // initialize default limits
                Dictionary<string, int> locLimits = new()
                {
                    ["x1"] = 1,
                    ["x2"] = int.MaxValue,
                    ["y1"] = 1,
                    ["y2"] = int.MaxValue,
                };
                try
                {
                    MatchCollection matches = Regex.Matches(location);
                    if (matches.Count == 1)
                    {
                        Match match = matches[0];
                        parseloc = location[..^match.Value.Length];
                        locLimits.Update(match, namedOnly: true);
                        this.Monitor.DebugOnlyLog($"Found and parsed sub-location: {parseloc} + ({locLimits["x1"]};{locLimits["y1"]});({locLimits["x2"]};{locLimits["y2"]})");
                    }
                    else if (matches.Count >= 2)
                    {
                        this.Monitor.Log(I18n.ExcessRegexMatches(loc: location), LogLevel.Warn);
                        continue;
                    }
                }
                catch (RegexMatchTimeoutException ex)
                {
                    this.Monitor.Log(I18n.RegexTimeout(loc: location, ex: ex), LogLevel.Warn);
                }

                if (Game1.getLocationFromName(parseloc) is GameLocation gameLocation)
                {
                    this.Monitor.DebugOnlyLog($"Found {gameLocation.NameOrUniqueName}");

                    (Vector2[] tiles, int num) = gameLocation.GetTiles(xstart: locLimits["x1"], xend: locLimits["x2"], ystart: locLimits["y1"], yend: locLimits["y2"]);
                    if (num == 0)
                    {
                        continue;
                    }

                    foreach (Vector2 tile in new Span<Vector2>(tiles).Shuffled(num, this.Random))
                    {
                        if (this.CanSpawnFruitHere(gameLocation, tile))
                        {
                            this.PlaceFruit(gameLocation, tile);
                            if (++count >= this.config.MaxDailySpawns)
                            {
                                ArrayPool<Vector2>.Shared.Return(tiles);
                                goto END;
                            }
                        }
                    }

                    ArrayPool<Vector2>.Shared.Return(tiles);
                }
                else
                {
                    this.Monitor.Log(I18n.LocationMissing(loc: location), LogLevel.Trace);
                }
            }
        }

        if (this.config.UseMineCave && Game1.getLocationFromName("Mine") is Mine mine)
        {
            (Vector2[] tiles, int num) = mine.GetTiles(xstart: 11);
            if (num > 0)
            {
                foreach (Vector2 tile in new Span<Vector2>(tiles).Shuffled(num, this.Random))
                {
                    if (this.CanSpawnFruitHere(mine, tile))
                    {
                        this.PlaceFruit(mine, tile);
                        if (++count >= this.config.MaxDailySpawns)
                        {
                            ArrayPool<Vector2>.Shared.Return(tiles);
                            goto END;
                        }
                    }
                }
                ArrayPool<Vector2>.Shared.Return(tiles);
            }
        }

END:
        this.ResetRandom();
        return;
    }

    /// <summary>
    /// Place a fruit on a specific tile.
    /// </summary>
    /// <param name="location">Map to place fruit on.</param>
    /// <param name="tile">Tile to place fruit on.</param>
    private void PlaceFruit(GameLocation location, Vector2 tile)
    {
        string fruitToPlace = this.Random.ChooseFrom(this.TreeFruit.Count > 0 && this.Random.OfChance(this.config.TreeFruitChance / 100f) ? this.TreeFruit : this.BASE_FRUIT);

        SObject fruit = ItemRegistry.Create<SObject>(fruitToPlace);
        fruit.IsSpawnedObject = true;
        location.setObject(tile, fruit);
        this.Monitor.DebugOnlyLog($"Spawning item {fruitToPlace} at {location.Name}:{tile.X},{tile.Y}", LogLevel.Debug);
    }

    [MethodImpl(TKConstants.Hot)]
    private bool CanSpawnFruitHere(GameLocation location, Vector2 tile)
        => this.Random.OfChance(this.config.SpawnChance / 100f)
            && location.IsTileViewable(new XLocation((int)tile.X, (int)tile.Y), Game1.viewport)
            && location.CanItemBePlacedHere(tile, ignorePassables: CollisionMask.Flooring | CollisionMask.Furniture);

    /// <summary>
    /// Console command to list valid fruits for spawning.
    /// </summary>
    /// <param name="command">Name of command.</param>
    /// <param name="args">Arguments for command.</param>
    private void ListFruits(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("World is not ready. Please load save first.", LogLevel.Info);
            return;
        }

        List<string> fruitNames = [];
        foreach (string objectID in this.GetTreeFruits())
        {
            if (Game1Wrappers.ObjectData.TryGetValue(objectID, out StardewValley.GameData.Objects.ObjectData? data))
            {
                fruitNames.Add(TokenParser.ParseText(data.DisplayName));
            }
        }
        StringBuilder sb = StringBuilderCache.Acquire(fruitNames.Count * 6);
        sb.Append("Possible fruits: ");
        sb.AppendJoin(", ", AtraUtils.ContextSort(fruitNames));
        this.Monitor.Log(StringBuilderCache.GetStringAndRelease(sb), LogLevel.Info);
    }

    /// <summary>
    /// Get data from assets, based on which mods are installed.
    /// </summary>
    /// <param name="datalocation">asset name.</param>
    /// <returns>List of data, split by commas.</returns>
    private List<string> GetData(IAssetName datalocation)
    {
        IDictionary<string, string> rawlist = this.Helper.GameContent.Load<Dictionary<string, string>>(datalocation);
        List<string> datalist = [];

        foreach (string uniqueID in rawlist.Keys)
        {
            if (this.Helper.ModRegistry.IsLoaded(uniqueID))
            {
                datalist.AddRange(rawlist[uniqueID].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
        }
        return datalist;
    }

    /// <summary>
    /// Generate list of tree fruits valid for spawning, based on user config/deny list/data in Data/fruitTrees.
    /// </summary>
    /// <returns>A list of tree fruit.</returns>
    private List<string> GetTreeFruits()
    {
        this.Monitor.DebugOnlyLog("Generating tree fruit list");

        if (this.config.UseVanillaFruitOnly)
        {
            return this.VANILLA_FRUIT;
        }

        List<string> denylist = this.GetData(AssetManager.DENYLIST_LOCATION);
        List<string> treeFruits = new(Game1.fruitTreeData.Count / 2);

        bool enforceSeason = this.config.SeasonalOnly == SeasonalBehavior.SeasonalOnly || (this.config.SeasonalOnly == SeasonalBehavior.SeasonalExceptWinter && !Game1.IsWinter);
        foreach ((string saplingIndex, FruitTreeData? tree) in Game1.fruitTreeData)
        {
            if (this.config.ProgressionMode && !InventoryWatcher.HaveSeen(saplingIndex))
            {
                continue;
            }

            if (enforceSeason && !tree.Seasons.Contains(Game1.season))
            {
                continue;
            }

            foreach (FruitTreeFruitData? candidate in tree.Fruit)
            {
                try
                {
                    if (this.ProcessFruitTreFruitData(candidate) is not SObject item)
                    {
                        continue;
                    }

                    if (!item.HasTypeObject())
                    {
                        continue;
                    }

                    // 73 is the golden walnut. Let's not let players have that, or 858's Qi gems.
                    if (item.QualifiedItemId is "(O)73" or "(O)858")
                    {
                        continue;
                    }

                    if (!this.config.AllowAnyTreeProduct && item.Category != SObject.FruitsCategory)
                    {
                        continue;
                    }

                    if (this.config.EdiblesOnly && item.Edibility < 0)
                    {
                        continue;
                    }

                    if (this.config.PriceCap > item.salePrice())
                    {
                        continue;
                    }

                    if (denylist.Contains(item.Name) || denylist.Contains(item.ItemId))
                    {
                        continue;
                    }

                    if (this.config.NoBananasBeforeShrine && item.QualifiedItemId == "(O)91"
                        && Game1.getLocationFromName("IslandEast") is IslandEast islandeast && !islandeast.bananaShrineComplete.Value)
                    {
                        continue;
                    }

                    treeFruits.Add(item.QualifiedItemId);
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"Ran into issue looking up item {candidate.Id} for tree {saplingIndex}\n{ex}", LogLevel.Warn);
                }
            }
        }
        return treeFruits;
    }

    // gets the fruit item associated with a specific fruit tree data drop.
    // derived from FruitTree.TryCreateFruit
    private SObject? ProcessFruitTreFruitData(FruitTreeFruitData data, bool enforceSeason = false)
    {
        if (!this.Random.OfChance(data.Chance))
        {
            return null;
        }
        if (data.Condition is not null
            && !GameStateQuery.CheckConditions(data.Condition, Game1.getFarm(), null, null, null, this.Random, enforceSeason ? null : GameStateQuery.SeasonQueryKeys))
        {
            return null;
        }
        if (enforceSeason && data.Season.HasValue && data.Season != Game1.season)
        {
            return null;
        }

        SObject? item = ItemQueryResolver.TryResolveRandomItem(data, new ItemQueryContext(Game1.getFarm(), null, null), avoidRepeat: false, null, null, null, delegate (string query, string error)
        {
            this.Monitor.Log($"Failed parsing item query '{query}' for drop ID {data.Id}, skipping. Error '{error}'");
        }) as SObject;
        if (item is not null)
        {
            if (item.Stack <= 0)
            {
                return null;
            }
            item.Stack = 1;
            item.Quality = SObject.lowQuality;
        }
        return item;
    }

    private void BellsAndWhistles(object? sender, OneSecondUpdateTickingEventArgs e)
    {
        if (Game1.currentLocation is Mine mine
            && this.SpawnedFruitToday
            && this.config.UseMineCave)
        { // The following code is copied out of the game and adds the bat sprites to the mines.
            if (Random.Shared.OfChance(0.12))
            {
                TemporaryAnimatedSprite redbat = new(
                    textureName: @"LooseSprites\Cursors",
                    sourceRect: new Rectangle(640, 1644, 16, 16),
                    animationInterval: 80f,
                    animationLength: 4,
                    numberOfLoops: 9999,
                    position: new Vector2(Random.Shared.Next(mine.map.Layers[0].LayerWidth), Random.Shared.Next(mine.map.Layers[0].LayerHeight)),
                    flicker: false,
                    flipped: false,
                    layerDepth: 1f,
                    alphaFade: 0f,
                    color: Color.Black,
                    scale: 4f,
                    scaleChange: 0f,
                    rotation: 0f,
                    rotationChange: 0f)
                {
                    xPeriodic = true,
                    xPeriodicLoopTime = 2000f,
                    xPeriodicRange = 64f,
                    motion = new Vector2(0f, -8f),
                };
                mine.TemporarySprites.Add(redbat);
                if (Random.Shared.OfChance(0.15))
                {
                    mine.localSound("batScreech");
                }
                for (int i = 0; i < 4; i++)
                {
                    DelayedAction.playSoundAfterDelay("batFlap", (320 * i) + 240);
                }
            }
            else if (Random.Shared.OfChance(0.24))
            {
                BatTemporarySprite batsprite = new(
                    new Vector2(
                        Random.Shared.OfChance(0.5) ? 0 : mine.map.DisplayWidth - 64,
                        mine.map.DisplayHeight - 64));
                mine.TemporarySprites.Add(batsprite);
            }
        }
    }

    #region migration

    /// <summary>
    /// Raised when save is loaded.
    /// </summary>
    /// <param name="sender">Unknown, used by SMAPI.</param>
    /// <param name="e">Parameters.</param>
    /// <remarks>Used to load in this mod's data models.</remarks>
    private void SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsSplitScreen && Context.ScreenId != 0)
        {
            return;
        }
        this.migrator = new(this.ModManifest, this.Helper, this.Monitor);
        if (!this.migrator.CheckVersionInfo())
        {
            this.Helper.Events.GameLoop.Saved += this.WriteMigrationData;
        }
        else
        {
            this.migrator = null;
        }
    }

    /// <summary>
    /// Writes migration data then detaches the migrator.
    /// </summary>
    /// <param name="sender">Smapi thing.</param>
    /// <param name="e">Arguments for just-before-saving.</param>
    private void WriteMigrationData(object? sender, SavedEventArgs e)
    {
        if (this.migrator is not null)
        {
            this.migrator.SaveVersionInfo();
            this.migrator = null;
        }
        this.Helper.Events.GameLoop.Saved -= this.WriteMigrationData;
    }

    #endregion
}
