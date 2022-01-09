using System.Globalization;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;
using StardewValley.Locations;


namespace FarmCaveSpawn;

public class ModEntry : Mod
{
    //The config is set by the Entry method, so it should never realistically be null
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private ModConfig config;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private readonly AssetManager assetManager = new();

    /// <summary>
    /// The item IDs for the four basic forage fruit.
    /// </summary>
    private readonly List<int> BaseFruit = new() { 296, 396, 406, 410 };

    /// <summary>
    /// Item IDs for items produced by trees.
    /// </summary>
    private List<int> TreeFruit = new();

    /// <summary>
    /// Location to temporarily store the seeded random, set in <seealso cref="GetRandom"/>
    /// </summary>
    private Random? random;
    
    /// <summary>
    /// Sublocation-parsing regex.
    /// </summary>
    private readonly Regex regex = new(
        //":[(x1;y1);(x2;y2)]"
        pattern: @":\[\((?<x1>\d+);(?<y1>\d+)\);\((?<x2>\d+);(?<y2>\d+)\)\]$", 
        options: RegexOptions.CultureInvariant|RegexOptions.Compiled, 
        new TimeSpan(1000000)
        );

    public override void Entry(IModHelper helper)
    {
#if DEBUG
        this.Monitor.Log("FarmCaveSpawn initializing, DEBUG mode. Do not release this version", LogLevel.Warn);
#endif
        I18n.Init(helper.Translation);
        this.config = this.Helper.ReadConfig<ModConfig>();

        helper.Events.GameLoop.DayStarted += this.SpawnFruit;
        helper.Events.GameLoop.GameLaunched += this.SetUpConfig;
        helper.ConsoleCommands.Add(
            name: "list_fruits",
            documentation: I18n.ListFruits_Description(),
            callback: this.ListFruits
            );
        helper.Content.AssetLoaders.Add(this.assetManager);
    }

    /// <summary>
    /// Remove the list TreeFruit when no longer necessary, delete the Random as well
    /// </summary>
    private void Cleanup()
    {
        this.TreeFruit.Clear();
        this.TreeFruit.TrimExcess();
        this.random = null;
    }

    //config should never be null anyways.
#pragma warning disable CS8605 // Unboxing a possibly null value.
    /// <summary>
    /// Generates the GMCM for this mod by looking at the structure of the config class.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <remarks>To add a new setting, add the details to the i18n file. Currently handles: bool, int, float</remarks>
    private void SetUpConfig(object? sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
    {
        var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null)
            return;

        configMenu.Register(
            mod: this.ModManifest,
            reset: () => this.config = new ModConfig(),
            save: () => this.Helper.WriteConfig(this.config));

        configMenu.AddParagraph(
            mod: this.ModManifest,
            text: I18n.Mod_Description
            );

        foreach (System.Reflection.PropertyInfo property in typeof(ModConfig).GetProperties())
        {
            if (property.PropertyType.Equals(typeof(bool)))
            {
                configMenu.AddBoolOption(
                    mod: this.ModManifest,
                    getValue: () => (bool)property.GetValue(this.config),
                    setValue: (bool value) => property.SetValue(this.config, value),
                    name: () => I18n.GetByKey($"{property.Name}.title"),
                    tooltip: () => I18n.GetByKey($"{property.Name}.description")
                   );
            }
            else if (property.PropertyType.Equals(typeof(int)))
            {
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    getValue: () => (int)property.GetValue(this.config),
                    setValue: (int value) => property.SetValue(this.config, value),
                    name: () => I18n.GetByKey($"{property.Name}.title"),
                    tooltip: () => I18n.GetByKey($"{property.Name}.description"),
                    min: 0,
                    interval: 1
                );
            }
            else if (property.PropertyType.Equals(typeof(float)))
            {
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    getValue: () => (float)property.GetValue(this.config),
                    setValue: (float value) => property.SetValue(this.config, value),
                    name: () => I18n.GetByKey($"{property.Name}.title"),
                    tooltip: () => I18n.GetByKey($"{property.Name}.description"),
                    min: 0.0f
                );
            }
            else { this.DebugLog($"{property.Name} unaccounted for.", LogLevel.Warn); }
        }
    }
#pragma warning restore CS8605 // Unboxing a possibly null value.

    /// <summary>
    /// gets a seeded random based on uniqueID and days played
    /// </summary>
    private Random GetRandom()
    {
        this.random = new((int)Game1.uniqueIDForThisGame * 2 + (int)Game1.stats.DaysPlayed * 7);
        return this.random;
    }

    /// <summary>
    /// Handle spawning fruit at the start of each day
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SpawnFruit(object? sender, StardewModdingAPI.Events.DayStartedEventArgs e)
    {
        // Compat for Farm Cave Framework: https://www.nexusmods.com/stardewvalley/mods/10506
        // Which saves the farm cave choice to their own SaveData, and doesn't update the MasterPlayer.caveChoice
        bool hasFCFbatcave = false;
        if (Game1.CustomData.TryGetValue("smapi/mod-data/aedenthorn.farmcaveframework/farm-cave-framework-choice", out string? farmcavechoice))
        {
            hasFCFbatcave = (farmcavechoice is not null) && (farmcavechoice.ToLowerInvariant().Contains("bat") || farmcavechoice.ToLowerInvariant().Contains("fruit"));
            this.DebugLog(hasFCFbatcave? "FarmCaveFramework fruit bat cave detected.": "FarmCaveFramework fruit bat cave not detected.");
        }

        if (!Context.IsMainPlayer) { return; }
        if (!this.config.EarlyFarmCave && (Game1.MasterPlayer.caveChoice?.Value is null || Game1.MasterPlayer.caveChoice.Value <= 0) && string.IsNullOrWhiteSpace(farmcavechoice))
        {
            this.DebugLog("Demetrius cutscene not seen and config not set to early, skip spawning for today.");
            return;
        }
        if (!this.config.IgnoreFarmCaveType && !this.config.EarlyFarmCave && (Game1.MasterPlayer.caveChoice?.Value is null || Game1.MasterPlayer.caveChoice.Value != 1) && !hasFCFbatcave)
        {
            this.DebugLog("Fruit bat cave not selected and config not set to ignore that, skip spawning for today.");
            return;
        }
        int count = 0;
        this.TreeFruit = this.GetTreeFruits();
        this.GetRandom();

        if (Game1.getLocationFromName("FarmCave") is FarmCave farmcave)
        {
            this.DebugLog($"Spawning in the farmcave");
            foreach (Vector2 v in this.IterateTiles(farmcave))
            {
                this.PlaceFruit(farmcave, v);
                if (++count >= this.config.MaxDailySpawns) { break; }
            }
            farmcave.UpdateReadyFlag();
            if (count >= this.config.MaxDailySpawns) { this.Cleanup(); return; }
        }

        if (this.config.UseModCaves)
        {
            foreach (string location in this.GetData(this.assetManager.additionalLocationsLocation))
            {
                string parseloc = location;
                //initialize default limits
                Dictionary<string, int> locLimits = new()
                {
                    ["x1"] = 1,
                    ["x2"] = int.MaxValue,
                    ["y1"] = 1,
                    ["y2"] = int.MaxValue,
                };
                try
                {
                    MatchCollection matches = this.regex.Matches(location);
                    if (matches.Count == 1)
                    {
                        Match match = matches[0];
                        parseloc = location[..^match.Value.Length];
                        foreach (Group group in match.Groups)
                        {
                            if (int.TryParse(group.Value, out int result))
                            {
                                locLimits[group.Name] = result;
                            }
                        }
                        this.DebugLog($"Found and parsed sublocation: {parseloc} + ({locLimits["x1"]};{locLimits["y1"]});({locLimits["x2"]};{locLimits["y2"]})");
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
                    this.Monitor.Log($"Found {gameLocation}");
                    foreach (Vector2 v in this.IterateTiles(gameLocation, xstart: locLimits["x1"], xend: locLimits["x2"], ystart: locLimits["y1"], yend: locLimits["y2"]))
                    {
                        this.PlaceFruit(gameLocation, v);
                        if (++count >= this.config.MaxDailySpawns) { this.Cleanup(); return; }
                    }
                }
                else
                {
                    this.Monitor.Log(I18n.LocationMissing(loc: location), LogLevel.Debug);
                }
            }
        }

        if (this.config.UseMineCave && Game1.getLocationFromName("Mine") is Mine mine)
        {
            foreach (Vector2 v in this.IterateTiles(mine, xstart: 11))
            {
                this.PlaceFruit(mine, v);
                if (++count >= this.config.MaxDailySpawns) { this.Cleanup(); return; }
            }
        }
    }

    public void PlaceFruit(GameLocation location, Vector2 tile)
    {
        if (this.random is null) { this.GetRandom(); }
        int fruitToPlace = Utility.GetRandom(this.random!.NextDouble() < (this.config.TreeFruitChance / 100f) ? this.TreeFruit : this.BaseFruit, this.random);
        location.setObject(tile, new StardewValley.Object(fruitToPlace, 1)
        {
            IsSpawnedObject = true
        });
        this.DebugLog($"Spawning item {fruitToPlace} at {location.Name}:{tile.X},{tile.Y}", LogLevel.Debug);
    }

    /// <summary>
    /// Iterate over tiles in a map, with a random chance to pick each tile.
    /// Will only return clear and placable tiles.
    /// </summary>
    /// <param name="location">Map to iterate over</param>
    /// <param name="xstart"></param>
    /// <param name="xend"></param>
    /// <param name="ystart"></param>
    /// <param name="yend"></param>
    /// <returns></returns>
    public IEnumerable<Vector2> IterateTiles(GameLocation location, int xstart = 1, int xend = int.MaxValue, int ystart = 1, int yend = int.MaxValue)
    {
        if (this.random is null) { this.GetRandom(); }
        foreach (int x in Enumerable.Range(xstart, Math.Clamp(xend, xstart, location.Map.Layers[0].LayerWidth - 2)).OrderBy((x) => this.random!.Next()))
        {
            foreach (int y in Enumerable.Range(ystart, Math.Clamp(yend, ystart, location.Map.Layers[0].LayerHeight - 2)).OrderBy((x) => this.random!.Next()))
            {
                Vector2 v = new(x, y);
                if (this.random!.NextDouble() < (this.config.SpawnChance / 100f) && location.isTileLocationTotallyClearAndPlaceableIgnoreFloors(v))
                {
                    yield return v;
                }
            }
        }
    }

    /// <summary>
    /// Console command to list valid fruits for spawning
    /// </summary>
    /// <param name="command"></param>
    /// <param name="args"></param>
    private void ListFruits(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("World is not ready. Please load save first.");
            return;
        }
        List<string> FruitNames = new();
        foreach (int objectID in this.GetTreeFruits())
        {
            StardewValley.Object obj = new(objectID, 1);
            FruitNames.Add(obj.DisplayName);
        }
        LocalizedContentManager contextManager = Game1.content;
        string langcode = contextManager.LanguageCodeString(contextManager.GetCurrentLanguage());
        FruitNames.Sort(StringComparer.Create(new CultureInfo(langcode), true));
        this.Monitor.Log($"Possible fruits: {String.Join(", ", FruitNames)}", LogLevel.Info);
    }

    /// <summary>
    /// Get data from assets, based on which mods are installed
    /// </summary>
    /// <param name="datalocation">asset name</param>
    /// <returns></returns>
    private List<string> GetData(string datalocation)
    {
        IDictionary<string, string> rawlist = this.Helper.Content.Load<Dictionary<string, string>>(datalocation, ContentSource.GameContent);
        List<string> datalist = new();

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
    /// Generate list of tree fruits valid for spawning, based on user config/denylist/data in Data/fruitTrees
    /// </summary>
    /// <returns></returns>
    private List<int> GetTreeFruits()
    {

        List<string> denylist = this.GetData(this.assetManager.denylistLocation);

        List<int> TreeFruits = new();
        Dictionary<int, string> fruittrees = this.Helper.Content.Load<Dictionary<int, string>>("Data/fruitTrees", ContentSource.GameContent);
        string currentseason = Game1.currentSeason.ToLower().Trim();
        foreach (string tree in fruittrees.Values)
        {
            string[] treedata = tree.Split('/');
            if (this.config.SeasonalOnly && Context.IsWorldReady)
            {
                if (!treedata[1].Contains(currentseason))
                {
                    if (!currentseason.Contains("summer") || !treedata[1].Contains("island"))
                    {
                        continue;
                    }
                }
            }

            if (int.TryParse(treedata[2].Trim(), out int objectIndex))
            {
                try
                {
                    StardewValley.Object fruit = new(objectIndex, 1);
                    if (!this.config.AllowAnyTreeProduct && fruit.Category != StardewValley.Object.FruitsCategory)
                    {
                        continue;
                    }
                    if (this.config.EdiblesOnly && fruit.Edibility < 0)
                    {
                        continue;
                    }
                    if (fruit.Price > this.config.PriceCap)
                    {
                        continue;
                    }
                    if (denylist.Contains(fruit.Name))
                    {
                        continue;
                    }
                    if (this.config.NoBananasBeforeShrine && fruit.Name.Equals("Banana"))
                    {
                        if (!Context.IsWorldReady) { continue; }
                        if (Game1.getLocationFromName("IslandEast") is IslandEast islandeast && !islandeast.bananaShrineComplete.Value) { continue; }
                    }
                    TreeFruits.Add(objectIndex);
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"Ran into issue looking up item {objectIndex}\n{ex}", LogLevel.Warn);
                }
            }
        }
        return TreeFruits;
    }

    /// <summary>
    /// Log to DEBUG if compiled with DEBUG
    /// Log to verbose only otherwise.
    /// </summary>
    /// <param name="message"></param>
    private void DebugLog(string message, LogLevel level = LogLevel.Debug)
    {
#if DEBUG
        this.Monitor.Log(message, level);
#else
        Monitor.VerboseLog(message);
#endif
    }

}
