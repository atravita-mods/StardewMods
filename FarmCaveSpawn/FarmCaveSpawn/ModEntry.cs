using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FarmCaveSpawn
{

    class ModConfig
    {
        public int MaxDailySpawns { get; set; } = 6; //Maximum number of spawns.

        public float SpawnChance { get; set; } = 5f; //probability of any tile spawning a thing.
        public float TreeFruitChance { get; set; } = 50f; //probability of the spawn being a tree fruit.
        public bool IgnoreFarmCaveType { get; set; } = false; //should I spawn fruits regardless of the farm cave type?
        public bool AllowAnyTreeProduct { get; set; } = true;
        public bool EdiblesOnly { get; set; } = true;
        public bool NoBananasBeforeShrine { get; set; } = true;
    }
    public class ModEntry: Mod
    {
        private ModConfig config;
        public override void Entry(IModHelper helper)
        {
            config = Helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.DayStarted += SpawnFruit;
            helper.Events.GameLoop.GameLaunched += SetUpConfig;
            helper.ConsoleCommands.Add("list_fruits", helper.Translation.Get("list-fruits.description"), this.ListFruits);
        }

        private void SetUpConfig(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => config = new ModConfig(),
                save: () => Helper.WriteConfig(config));

            configMenu.AddParagraph(
                mod:ModManifest,
                text: () => Helper.Translation.Get("mod.description")
                );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => config.MaxDailySpawns,
                setValue: value => config.MaxDailySpawns = value,
                name: () => Helper.Translation.Get("max-spawns.title"),
                tooltip: () => Helper.Translation.Get("max-spawns.description"),
                min: 0,
                interval: 1
                );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => config.SpawnChance,
                setValue: value => config.SpawnChance = value,
                name: () => Helper.Translation.Get("spawn-chance.title"),
                tooltip: () => Helper.Translation.Get("spawn-chance.description"),
                min: 0.0f
                );

            configMenu.AddNumberOption(
                mod: ModManifest,
                getValue: () => config.TreeFruitChance,
                setValue: value => config.TreeFruitChance = value,
                name: () => Helper.Translation.Get("tree-fruit-chance.title"),
                tooltip: () => Helper.Translation.Get("tree-fruit-chance.description"),
                min: 0.0f
                );

            configMenu.AddBoolOption(
                mod: ModManifest,
                getValue: () => config.IgnoreFarmCaveType,
                setValue: value => config.IgnoreFarmCaveType = value,
                name: () => Helper.Translation.Get("cave-type.title"),
                tooltip: () => Helper.Translation.Get("cave-type.description")
                );

            configMenu.AddBoolOption(
                mod: ModManifest,
                getValue: () => config.AllowAnyTreeProduct,
                setValue: value => config.AllowAnyTreeProduct = value,
                name: () => Helper.Translation.Get("any-category.title"),
                tooltip: () => Helper.Translation.Get("any-category.dsecription")
                );

            configMenu.AddBoolOption(
                mod: ModManifest,
                getValue: () =>config.EdiblesOnly,
                setValue: value => config.EdiblesOnly = value,
                name: () => Helper.Translation.Get("edibles.title"),
                tooltip: () => Helper.Translation.Get("edibles.description")
                );

            configMenu.AddBoolOption(
                mod: ModManifest,
                getValue: () => config.NoBananasBeforeShrine,
                setValue: value => config.NoBananasBeforeShrine = value,
                name: () => Helper.Translation.Get("nobananas.title"),
                tooltip: () => Helper.Translation.Get("nobananas.description")
                );
        }

        private void SpawnFruit(object sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            if (!Game1.IsMasterGame) { return; }
            int count = 0;
            List<int> TreeFruit = GetTreeFruits();
            List<int> BaseFruit = new() { 296, 396, 406, 410 };
            Random random = new((int)Game1.uniqueIDForThisGame * 2 + (int)Game1.stats.DaysPlayed * 7);
            FarmCave farmcave = Game1.getLocationFromName("FarmCave") as FarmCave;
            if (config.IgnoreFarmCaveType || Game1.MasterPlayer.caveChoice.Value == 1)
            {
                foreach (int x in Enumerable.Range(1, farmcave.Map.Layers[0].LayerWidth - 2).OrderBy((x)=> random.Next()))
                {
                    foreach (int y in Enumerable.Range(1, farmcave.Map.Layers[0].LayerHeight - 2).OrderBy((x) => random.Next()))
                    {
                        Vector2 v = new(x, y);
                        if (random.NextDouble() < (config.SpawnChance/100f) && farmcave.isTileLocationTotallyClearAndPlaceableIgnoreFloors(v) )
                        {
                            int fruitToPlace;
                            if (random.NextDouble() < (config.TreeFruitChance/100f))
                            {
                                fruitToPlace = Utility.GetRandom<int>(TreeFruit, random);
                            }
                            else
                            {
                                fruitToPlace = Utility.GetRandom<int>(BaseFruit, random);
                            }
                            farmcave.setObject(v, new StardewValley.Object(fruitToPlace, 1)
                            {
                                IsSpawnedObject = true
                            });
                            Monitor.Log($"Spawning item {fruitToPlace} at {x},{y}");
                            count++;
                            if (count >= config.MaxDailySpawns) {
                                farmcave.UpdateReadyFlag();
                                return; 
                            }
                        }
                    }
                }
                farmcave.UpdateReadyFlag();
            }
        }

        private void ListFruits(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                Monitor.Log("World is not ready. Please load save first.");
                return;
            }
            List<string> FruitNames = new();
            foreach (int objectID in GetTreeFruits())
            {
                StardewValley.Object obj = new(objectID, 1);
                FruitNames.Add(obj.DisplayName);
            }
            LocalizedContentManager contextManager = Game1.content;
            string langcode = contextManager.LanguageCodeString(contextManager.GetCurrentLanguage());
            FruitNames.Sort(StringComparer.Create(new CultureInfo(langcode), true));
            Monitor.Log($"Possible fruits: {String.Join(", ", FruitNames)}", LogLevel.Info);
        }

        private List<int> GetTreeFruits()
        {
            List<int> TreeFruits = new();
            Dictionary<int, string> fruittrees= Helper.Content.Load<Dictionary<int, string>>("Data/fruitTrees", ContentSource.GameContent);
            foreach (string tree in fruittrees.Values)
            {
                string[] treedata = tree.Split('/');
                bool success = int.TryParse(treedata[2].Trim(), out int objectIndex);
                if (success)
                {
                    try
                    {
                        StardewValley.Object fruit = new(objectIndex, 1);
                        if (config.AllowAnyTreeProduct || fruit.Category == StardewValley.Object.FruitsCategory)
                        {
                            if (!config.EdiblesOnly || fruit.Edibility >= 0)
                            {
                                if (config.NoBananasBeforeShrine && fruit.Name.Equals("Banana"))
                                {
                                    if (!Context.IsWorldReady) { continue; }
                                    IslandEast islandeast = Game1.getLocationFromName("IslandEast") as IslandEast;
                                    if (!islandeast.bananaShrineComplete.Value) { continue; }
                                }
                                TreeFruits.Add(objectIndex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Monitor.Log($"Ran into issue looking up item {objectIndex}\n{ex}", LogLevel.Warn);
                    }
                }
            }
            return TreeFruits;
        }
    }
}
