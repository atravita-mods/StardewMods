using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

using StardewModdingAPI;

using StardewValley;
using StardewValley.GameData;

namespace SpecialOrdersExtended
{
    class ModEntry : Mod
    {
        private static IMonitor ModMonitor;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = this.Monitor;

            Harmony harmony = new(this.ModManifest.UniqueID);

            harmony.Patch(
                original: typeof(SpecialOrder).GetMethod("CheckTag", BindingFlags.NonPublic | BindingFlags.Static),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.PrefixCheckTag))
                );
            ModMonitor.Log("Patching SpecialOrder:CheckTag", LogLevel.Debug);

            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), nameof(NPC.checkForNewCurrentDialogue)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.PostfixCheckDialogue))
                );
            ModMonitor.Log("Patching NPC:checkForNewCurrentDialogue for Special Orders Dialogue", LogLevel.Debug);

            helper.ConsoleCommands.Add("special_order_pool", "Lists the available special orders", this.GetAvailableOrders);
            helper.ConsoleCommands.Add("check_tag", "Check the current value of a tag", this.ConsoleCheckTag);
        }

        private static void PostfixCheckDialogue(ref bool __result, ref NPC __instance, ref int __0, ref bool __1)
        {
            try
            {
                if (__result) { return; } //have already found a New Current Dialogue
                foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
                {
                    string baseKey = (__1) ? specialOrder.questKey.Value: Game1.currentSeason + specialOrder.questKey.Value;
                    switch (specialOrder.questState.Value)
                    {
                        case SpecialOrder.QuestState.InProgress:
                            baseKey += "_InProgress";
                            break;
                        case SpecialOrder.QuestState.Failed:
                            baseKey += "_Failed";
                            break;
                        case SpecialOrder.QuestState.Complete:
                            baseKey += "_Completed";
                            break;
                    }

                    string dialogueKey = $"{baseKey}_{Game1.shortDayDisplayNameFromDayOfSeason(Game1.dayOfMonth)}";
                    if (__instance.Dialogue.ContainsKey(dialogueKey))
                    {
                        if (Game1.player.mailReceived.Contains($"{__instance.Name}_{dialogueKey}")) { continue; }
                        Game1.player.mailReceived.Add($"{__instance.Name}_{dialogueKey}");
                        __instance.CurrentDialogue.Push(new Dialogue(__instance.Dialogue[dialogueKey], __instance) { removeOnNextMove = true });
                        ModMonitor.Log($"Found key {dialogueKey}", LogLevel.Trace);
                        __result = true;
                        return;
                    }

                    for (int heartLevel = 14; heartLevel > 0; heartLevel -= 2)
                    {
                        dialogueKey = $"{baseKey}{heartLevel}";
                        if (__0 > heartLevel && __instance.Dialogue.ContainsKey(dialogueKey))
                        {
                            if (Game1.player.mailReceived.Contains($"{__instance.Name}_{dialogueKey}")) { continue; }
                            Game1.player.mailReceived.Add($"{__instance.Name}_{dialogueKey}");
                            __instance.CurrentDialogue.Push(new Dialogue(__instance.Dialogue[dialogueKey], __instance) { removeOnNextMove = true });
                            ModMonitor.Log($"Found key {dialogueKey}", LogLevel.Trace);
                            __result = true;
                            return;
                        }
                    }

                    if (__instance.Dialogue.ContainsKey(baseKey))
                    {
                        if (Game1.player.mailReceived.Contains($"{__instance.Name}_{baseKey}")) { continue; }
                        Game1.player.mailReceived.Add($"{__instance.Name}_{baseKey}");
                        __instance.CurrentDialogue.Push(new Dialogue(__instance.Dialogue[baseKey], __instance) { removeOnNextMove = true });
                        ModMonitor.Log($"Found key {baseKey}", LogLevel.Trace);
                        __result = true;
                        return;
                    }

                    ModMonitor.Log($"Did not find dialogue key for special order {baseKey} for NPC {__instance.Name}", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"Failed in checking for Special Order dialogue for NPC {__instance.Name}\n{ex}", LogLevel.Error);
            }
        }
        private static bool PrefixCheckTag(ref bool __result, ref string __0)
        {
            ModMonitor.VerboseLog($"Checking tag {__0}");
            try
            {
                if (__0.StartsWith("year_"))
                {
                    __result = Game1.year == int.Parse(__0["year_".Length..]);
                    return false;
                }
                else if (__0.StartsWith("atleastyear_"))
                {
                    __result = Game1.year >= int.Parse(__0["atleastyear_".Length..]);
                    return false;
                }
                else if (__0.StartsWith("yearunder_"))
                {
                    __result = Game1.year < int.Parse(__0["yearunder_".Length..]);
                    return false;
                }
                else if (__0.StartsWith("week_"))
                {
                    __result = __0 switch
                    {
                        "week_1" => (1 <= Game1.dayOfMonth) && (Game1.dayOfMonth <= 7),
                        "week_2" => (8 <= Game1.dayOfMonth) && (Game1.dayOfMonth <= 14),
                        "week_3" => (15 <= Game1.dayOfMonth) && (Game1.dayOfMonth <= 21),
                        "week_4" => (22 <= Game1.dayOfMonth) && (Game1.dayOfMonth <= 28),
                        _ => false,
                    };
                    return false;
                }
                else if (__0.StartsWith("daysplayed_"))
                {
                    string remainder = __0["daysplayed_".Length..];
                    if (!remainder.StartsWith("under_"))
                    {
                        __result = Game1.stats.DaysPlayed >= int.Parse(remainder);
                    }
                    else
                    {
                        __result = Game1.stats.DaysPlayed < int.Parse(remainder["under_".Length..]);
                    }
                    return false;
                }
                else if (__0.StartsWith("dropboxRoom_"))
                {
                    foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
                    {
                        if (specialOrder.questState.Value != SpecialOrder.QuestState.InProgress)
                        {
                            continue;
                        }
                        string roomname = __0["dropboxRoom_".Length..].Trim();
                        foreach (OrderObjective objective in specialOrder.objectives)
                        {
                            if (objective is DonateObjective && (objective as DonateObjective).dropBoxGameLocation.Value.Equals(roomname))
                            {
                                __result = true;
                                return false;
                            }
                        }
                    }
                    __result = false;
                    return false;
                }
                else if (__0.StartsWith("conversation_"))
                {
                    __result = false;
                    bool negate = false;
                    string remainder = __0["conversation_".Length..];
                    if (remainder.EndsWith("_not"))
                    {
                        negate = true;
                        remainder = remainder[0..^4];
                    }
                    foreach (Farmer farmer in Game1.getAllFarmers())
                    {
                        if (farmer.activeDialogueEvents.ContainsKey(remainder))
                        {
                            __result = true;
                            break;
                        }
                    }
                    if (negate) { __result = !__result; }
                    return false;
                }
                else if (__0.StartsWith("haskilled_"))
                {
                    __result = false;
                    string[] vals = __0.Split('_');
                    string monster = vals[1].Replace('-', ' ');
                    bool negate = false;
                    int kills_needed;
                    if (vals[2] == "under")
                    {
                        kills_needed = int.Parse(vals[3]);
                        negate = true;
                    }
                    else
                    {
                        kills_needed = int.Parse(vals[2]);
                    }
                    __result = Game1.getAllFarmers().Any( (Farmer farmer) => farmer.stats.getMonstersKilled(monster) >= kills_needed);
                    if (negate) { __result = !__result; }
                    return false;
                }
                else if (__0.StartsWith("friendship_")) //Consider marriage?
                {
                    __result = false;
                    string[] vals = __0.Split('_');
                    string friend = vals[1];
                    int friendship;
                    bool negate = false;
                    if (vals[2].Contains("under"))
                    {
                        friendship = int.Parse(vals[3]);
                        negate = true;
                    }
                    else
                    {
                        friendship = int.Parse(vals[2]);
                    }
                    __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.getFriendshipLevelForNPC(friend) >= friendship);
                    if (negate) { __result = !__result; }
                    return false;
                }
                else if (__0.StartsWith("minelevel_"))
                {
                    string remainder = __0["minelevel_".Length..];
                    if (!remainder.StartsWith("under_"))
                    {
                        __result = StardewValley.Locations.MineShaft.lowestLevelReached >= int.Parse(remainder);
                    }
                    else
                    {
                        __result = StardewValley.Locations.MineShaft.lowestLevelReached < int.Parse(remainder["under_".Length..]);
                    }
                    return false;
                }
                else if (__0.StartsWith("houselevel_"))
                {
                    __result = false;
                    string remainder = __0["houselevel_".Length..];
                    int houselevel;
                    bool negate = false;
                    if (remainder.StartsWith("under_"))
                    {
                        negate = true;
                        houselevel = int.Parse(remainder["under_".Length..]);
                    }
                    else
                    {
                        houselevel = int.Parse(remainder);
                    }
                    __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.HouseUpgradeLevel >= houselevel);
                    if (negate) { __result = !__result; }
                    return false;
                }
                else if (__0.StartsWith("moneyearned_"))
                {
                    string remainder = __0["moneyearned_".Length..];
                    if (!remainder.StartsWith("under_"))
                    {
                        __result = Game1.MasterPlayer.totalMoneyEarned >= uint.Parse(remainder);
                    }
                    else
                    {
                        __result = Game1.MasterPlayer.totalMoneyEarned < uint.Parse(remainder["under_".Length..]);
                    }
                    return false;
                }
                else if (__0.StartsWith("skilllevel_"))
                {
                    string[] vals = __0.Split('_');
                    int levelwanted;
                    bool negate = false;
                    if (vals[2] == "under")
                    {
                        levelwanted = int.Parse(vals[3]);
                        negate = true;
                    }
                    else
                    {
                        levelwanted = int.Parse(vals[2]);
                    }
                    __result = vals[1] switch
                    {
                        "mining" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.MiningLevel >= levelwanted),
                        "farming" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.FarmingLevel >= levelwanted),
                        "fishing" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.FishingLevel >= levelwanted),
                        "foraging" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.ForagingLevel >= levelwanted),
                        "combat" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.CombatLevel >= levelwanted),
                        _ => false,
                    };
                    if (negate) { __result = !__result; }
                    return false;
                }
                else if (__0.StartsWith("hasspecialitem_"))
                {
                    bool negate = false;
                    string searchtag;
                    if (__0.EndsWith("_not"))
                    {
                        negate = true;
                        searchtag = __0[0..^4];
                    }
                    else
                    {
                        searchtag = __0;
                    }
                    __result = searchtag switch
                    {
                        "hasspecialitem_clubCard" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.hasClubCard),
                        "hasspecialitem_specialCharm" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.hasSpecialCharm),
                        "hasspecialitem_skullKey" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.hasSkullKey),
                        "hasspecialitem_rustyKey" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.hasRustyKey),
                        "hasspecialitem_translationGuide" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.canUnderstandDwarves),
                        "hasspecialitem_townKey" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.HasTownKey),
                        _ => false,
                    };
                    if (negate) { __result = !__result; }
                    return false;
                }
                else if (__0.StartsWith("craftingrecipe_"))
                {
                    __result = false;
                    bool negate = false;
                    string remainder = __0["craftingrecipe_".Length..];
                    string recipe;
                    if (remainder.EndsWith("_not")){
                        negate = true;
                        recipe = remainder[0..^4].Replace('-', ' ');
                    }
                    else
                    {
                        recipe = remainder.Replace('-', ' ');
                    }
                    __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.craftingRecipes.ContainsKey(recipe));
                    if (negate) { __result = !__result; }
                    return false;
                }
                else if (__0.StartsWith("cookingrecipe_"))
                {
                    __result = false;
                    bool negate = false;
                    string remainder = __0["cookingrecipe_".Length..];
                    string recipe;
                    if (remainder.EndsWith("_not"))
                    {
                        negate = true;
                        recipe = remainder[0..^4].Replace('-', ' ');
                    }
                    else
                    {
                        recipe = remainder.Replace('-', ' ');
                    }
                    __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.cookingRecipes.ContainsKey(recipe));
                    if (negate) { __result = !__result; }
                    return false;
                }
                else if (__0.StartsWith("stats_"))
                {
                    string[] vals = __0.Split('_');
                    string statistic = vals[1];
                    bool negate = false;
                    uint value;
                    if (vals[2] == "under")
                    {
                        value = uint.Parse(vals[3]);
                        negate = true;
                    }
                    else
                    {
                        value = uint.Parse(vals[2]);
                    }
                    __result = statistic switch
                    {
                        "seedsSown" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.SeedsSown >= value),
                        "itemsShipped" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.ItemsShipped >= value),
                        "itemsCooked" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.ItemsCooked >= value),
                        "itemsCrafted" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.ItemsCrafted >= value),
                        "chickenEggsLayed" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.ChickenEggsLayed >= value),
                        "duckEggsLayed" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.DuckEggsLayed >= value),
                        "cowMilkProduced" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.CowMilkProduced >= value),
                        "goatMilkProduced" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.GoatMilkProduced >= value),
                        "rabbitWoolProduced" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.RabbitWoolProduced >= value),
                        "sheepWoolProduced" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.SheepWoolProduced >= value),
                        "cheeseMade" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.CheeseMade >= value),
                        "goatCheeseMade" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.goatCheeseMade >= value),
                        "trufflesFound" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.TrufflesFound >= value),
                        "stoneGathered" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.StoneGathered >= value),
                        "rocksCrushed" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.RocksCrushed >= value),
                        "dirtHowed" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.DirtHoed >= value),
                        "giftsGiven" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.GiftsGiven >= value),
                        "timesUnconscious" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.TimesUnconscious >= value),
                        "timesFished" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.TimesFished >= value),
                        "fishCaught" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.FishCaught >= value),
                        "bouldersCracked" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.BouldersCracked >= value),
                        "stumpsChopped" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.StumpsChopped >= value),
                        "stepsTaken" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.StepsTaken >= value),
                        "monstersKilled" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.MonstersKilled >= value),
                        "diamondsFound" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.DiamondsFound >= value),
                        "prismaticShardsFound" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.PrismaticShardsFound >= value),
                        "otherPreciousGemsFound" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.OtherPreciousGemsFound >= value),
                        "caveCarrotsFound" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.CaveCarrotsFound >= value),
                        "copperFound" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.CopperFound >= value),
                        "ironFound" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.IronFound >= value),
                        "coalFound" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.CoalFound >= value),
                        "coinsFound" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.CoinsFound >= value),
                        "goldFound" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.GoldFound >= value),
                        "iridiumFound" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.IridiumFound >= value),
                        "barsSmelted" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.BarsSmelted >= value),
                        "beveragesMade" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.BeveragesMade >= value),
                        "preservesMade" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.PreservesMade >= value),
                        "piecesOfTrashRecycled" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.PiecesOfTrashRecycled >= value),
                        "mysticStonesCrushed" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.MysticStonesCrushed >= value),
                        "daysPlayed" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.DaysPlayed >= value),
                        "weedsEliminated" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.WeedsEliminated >= value),
                        "sticksChopped" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.SticksChopped >= value),
                        "notesFound" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.NotesFound >= value),
                        "questsCompleted" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.QuestsCompleted >= value),
                        "starLevelCropsShipped" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.StarLevelCropsShipped >= value),
                        "cropsShipped" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.CropsShipped >= value),
                        "itemsForaged" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.ItemsForaged >= value),
                        "slimesKilled" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.SlimesKilled >= value),
                        "geodesCracked" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.GeodesCracked >= value),
                        "goodFriends" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.GoodFriends >= value),
                        "individualMoneyEarned" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.stats.IndividualMoneyEarned >= value),
                        _ => false,
                    };
                    if (negate) { __result = !__result; }
                    return false;
                }
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"Failed while checking tag {__0}\n{ex}", LogLevel.Error);
            }
            return true; //continue to base code.

        }

        private void ConsoleCheckTag(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                ModMonitor.Log("Load save first", LogLevel.Debug);
                return;
            }
            foreach (string tag in args)
            {
                string base_tag;
                bool match = true;
                if (tag.StartsWith("!"))
                {
                    match = false;
                    base_tag = tag.Trim()[1..];
                }
                else
                {
                    base_tag = tag.Trim();
                }
                bool res = match == Helper.Reflection.GetMethod(typeof(SpecialOrder), "CheckTag").Invoke<bool>(base_tag);
                if (res)
                {
                    ModMonitor.Log($"{tag}: True", LogLevel.Debug);
                }
                else
                {
                    ModMonitor.Log($"{tag}: False", LogLevel.Debug);
                }
            }
        }

        private void GetAvailableOrders(string command, string[] args)
        {
            if (!Context.IsWorldReady) { ModMonitor.Log($"Warning: save not loaded", LogLevel.Warn); }
            Dictionary<string, SpecialOrderData> order_data = Game1.content.Load <Dictionary<string, SpecialOrderData>>("Data\\SpecialOrders");
            List<string> keys = new(order_data.Keys);
            ModMonitor.Log($"{keys.Count} individual special orders found", LogLevel.Debug);

            List<string> validkeys = new();
            List<string> unseenkeys = new();

            foreach (string key in keys)
            {
                SpecialOrderData order = order_data[key];
                if (IsAvailableOrder(key, order))
                {
                    validkeys.Add(key);
                    if (!Game1.MasterPlayer.team.completedSpecialOrders.ContainsKey(key)) { unseenkeys.Add(key); }
                }
            }
            ModMonitor.Log($"{validkeys.Count} Valid Keys: {string.Join(", ", validkeys)}", LogLevel.Debug);
            ModMonitor.Log($"{unseenkeys.Count} Unseen Keys: {string.Join(", ", unseenkeys)}", LogLevel.Debug);
        }

        private bool IsAvailableOrder(string key, SpecialOrderData order)
        {
            ModMonitor.Log($"Analyzing {key}", LogLevel.Debug);
            try
            {
                SpecialOrder.GetSpecialOrder(key, Game1.random.Next());
                ModMonitor.Log($"    {key} seems parsable", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"    {key} has errors in parsing\n{ex}", LogLevel.Error);
                return false;
            }

            bool seen = Game1.MasterPlayer.team.completedSpecialOrders.ContainsKey(key);
            if (order.Repeatable != "True" && seen)
            {
                ModMonitor.Log($"    Nonrepeatable", LogLevel.Debug);
                return false;
            }
            else if (seen)
            {
                ModMonitor.Log($"    Repeatable, seen before", LogLevel.Debug);
            }
            if (Game1.dayOfMonth >= 16 && order.Duration == "Month")
            {
                ModMonitor.Log($"    Month long quest, not available after 16th");
                return false;
            }
            if (!SpecialOrder.CheckTags(order.RequiredTags))
            {
                ModMonitor.Log($"    Has invalid tags:", LogLevel.Debug);
                string[] tags = order.RequiredTags.Split(',', StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
                foreach (string tag in tags)
                {
                    bool match = true;
                    if (tag.Length == 0) { continue; }
                    string trimmed_tag;
                    if (tag.StartsWith("!"))
                    {
                        match = false;
                        trimmed_tag = tag[1..];
                    }
                    else
                    {
                        trimmed_tag = tag;
                    }

                    if(!(match == Helper.Reflection.GetMethod(typeof(SpecialOrder), "CheckTag").Invoke<bool>(trimmed_tag)))
                    {
                        ModMonitor.Log($"         Tag failed: {tag}", LogLevel.Debug);
                    }
                }
                return false;
            }
            foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
            {
                if (specialOrder.questKey.Value == key)
                {
                    ModMonitor.Log("    Currently active", LogLevel.Debug);
                    return false;
                }
            }
            return true;
        }
    }
}
