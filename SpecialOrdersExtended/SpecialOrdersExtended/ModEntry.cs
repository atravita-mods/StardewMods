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
        public static IMonitor ModMonitor;
        private static StatsManager StatsManager;
        public static ITranslationHelper I18n;
        public static IDataHelper DataHelper;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = this.Monitor;
            I18n = Helper.Translation;
            StatsManager = new();
            DataHelper = helper.Data;

            Harmony harmony = new(this.ModManifest.UniqueID);

            harmony.Patch(
                original: typeof(SpecialOrder).GetMethod("CheckTag", BindingFlags.NonPublic | BindingFlags.Static),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.PrefixCheckTag))
                );
            ModMonitor.Log("Patching SpecialOrder:CheckTag", LogLevel.Debug);

            try
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(NPC), nameof(NPC.checkForNewCurrentDialogue)),
                    postfix: new HarmonyMethod(typeof(DialogueManager), nameof(DialogueManager.PostfixCheckDialogue))
                    );
                ModMonitor.Log("Patching NPC:checkForNewCurrentDialogue for Special Orders Dialogue", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"Failed to patch NPC:checkForNewCurrentDialogue for Special Orders Dialogue\n\n{ex}", LogLevel.Error);
            }

            helper.ConsoleCommands.Add(
                name: "special_order_pool",
                documentation: I18n.Get("special_order_pool.description"),
                callback: this.GetAvailableOrders
                );
            helper.ConsoleCommands.Add(
                name: "check_tag",
                documentation: I18n.Get("check_tag.description"),
                callback: this.ConsoleCheckTag
                );
            helper.ConsoleCommands.Add(
                name: "list_available_stats",
                documentation: I18n.Get("list_available_stats.description"),
                callback: StatsManager.ConsoleListProperties
                );
            helper.ConsoleCommands.Add(
                name: "special_orders_dialogue",
                documentation: $"{I18n.Get("special_orders_dialogue.description")}\n\n{I18n.Get("special_orders_dialogue.usage")}\n    {I18n.Get("special_orders_dialogue.example")}",
                callback: DialogueManager.ConsoleSpecialOrderDialogue
                );

            helper.Events.GameLoop.SaveCreated += ClearCaches;
        }

        private void ClearCaches(object sender, StardewModdingAPI.Events.SaveCreatedEventArgs e)
        {
            StatsManager.ClearProperties();
        }

        private static bool PrefixCheckTag(ref bool __result, string __0)
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
                    string[] vals = __0.Split('_');
                    if (vals[1].Equals("under", StringComparison.OrdinalIgnoreCase))
                    {
                        __result = Game1.stats.DaysPlayed < int.Parse(vals[2]);
                    }
                    else
                    {
                        __result = Game1.stats.DaysPlayed >= int.Parse(vals[1]);
                    }
                    return false;
                }
                else if (__0.StartsWith("dropboxRoom_"))
                {
                    string roomname = __0["dropboxRoom_".Length..];
                    foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
                    {
                        if (specialOrder.questState.Value != SpecialOrder.QuestState.InProgress) { continue; }
                        foreach (OrderObjective objective in specialOrder.objectives)
                        {
                            if (objective is DonateObjective && (objective as DonateObjective).dropBoxGameLocation.Value.Equals(roomname, StringComparison.OrdinalIgnoreCase))
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
                    bool negate = false;
                    string[] vals = __0.Split('_');
                    string conversationTopic = vals[1];
                    if (vals.Length >= 3 && vals[2].Equals("not", StringComparison.OrdinalIgnoreCase)) {negate = true; }
                    __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.activeDialogueEvents.ContainsKey(conversationTopic));
                    if (negate) { __result = !__result; }
                    return false;
                }
                else if (__0.StartsWith("haskilled_"))
                {
                    string[] vals = __0.Split('_');
                    string monster = vals[1].Replace('-', ' ');
                    bool negate = false;
                    int kills_needed;
                    if (vals[2].Equals("under", StringComparison.OrdinalIgnoreCase))
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
                    string[] vals = __0.Split('_');
                    int friendship;
                    bool negate = false;
                    if (vals[2].Equals("under", StringComparison.OrdinalIgnoreCase))
                    {
                        friendship = int.Parse(vals[3]);
                        negate = true;
                    }
                    else
                    {
                        friendship = int.Parse(vals[2]);
                    }
                    __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.getFriendshipLevelForNPC(vals[1]) >= friendship);
                    if (negate) { __result = !__result; }
                    return false;
                }
                else if (__0.StartsWith("minelevel_"))
                {
                    string[] vals = __0.Split('_');
                    if(vals[1].Equals("under", StringComparison.OrdinalIgnoreCase))
                    {
                        __result = Utility.GetAllPlayerDeepestMineLevel() < int.Parse(vals[2]);
                    }
                    else
                    {
                        __result = Utility.GetAllPlayerDeepestMineLevel() >= int.Parse(vals[1]);
                    }
                    return false;
                }
                else if (__0.StartsWith("houselevel_"))
                {
                    string[] vals = __0.Split('_');
                    if (vals[1].Equals("under", StringComparison.OrdinalIgnoreCase))
                    {
                        __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.HouseUpgradeLevel < int.Parse(vals[2]));
                    }
                    else
                    {
                        __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.HouseUpgradeLevel >= int.Parse(vals[1]));
                    }
                    return false;
                }
                else if (__0.StartsWith("moneyearned_"))
                {
                    string[] vals = __0.Split('_');
                    if (vals[1].Equals("under", StringComparison.OrdinalIgnoreCase))
                    {
                        __result = Game1.MasterPlayer.totalMoneyEarned < uint.Parse(vals[2]);
                    }
                    else
                    {
                        __result = Game1.MasterPlayer.totalMoneyEarned >= uint.Parse(vals[1]);
                    }
                    return false;
                }
                else if (__0.StartsWith("skilllevel_"))
                {
                    string[] vals = __0.Split('_');
                    int levelwanted;
                    bool negate = false;
                    if (vals[2].Equals("under", StringComparison.OrdinalIgnoreCase))
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
                    string[] vals = __0.Split('_');
                    string searchtag = vals[1];
                    if (vals.Length >= 3 && vals[2].Equals("not", StringComparison.OrdinalIgnoreCase)){ negate = true; }
                    __result = searchtag switch
                    {
                        "clubCard" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.hasClubCard),
                        "specialCharm" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.hasSpecialCharm),
                        "skullKey" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.hasSkullKey),
                        "rustyKey" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.hasRustyKey),
                        "translationGuide" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.canUnderstandDwarves),
                        "townKey" => Game1.getAllFarmers().Any((Farmer farmer) => farmer.HasTownKey),
                        _ => false,
                    };
                    if (negate) { __result = !__result; }
                    return false;
                }
                else if (__0.StartsWith("craftingrecipe_"))
                {
                    bool negate = false;
                    string[] vals = __0.Split('_');
                    if (vals.Length >= 3 && vals[2].Equals("not", StringComparison.OrdinalIgnoreCase)) { negate = true; }
                    __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.craftingRecipes.ContainsKey(vals[1].Replace('-', ' ')));
                    if (negate) { __result = !__result; }
                    return false;
                }
                else if (__0.StartsWith("cookingrecipe_"))
                {
                    bool negate = false;
                    string[] vals = __0.Split('_');
                    if (vals.Length >= 3 && vals[2].Equals("not", StringComparison.OrdinalIgnoreCase)) { negate = true; }
                    __result = Game1.getAllFarmers().Any((Farmer farmer) => farmer.cookingRecipes.ContainsKey(vals[1].Replace('-', ' ')));
                    if (negate) { __result = !__result; }
                    return false;
                }
                else if (__0.StartsWith("stats_"))
                {
                    string[] vals = __0.Split('_');
                    string statistic = vals[1];
                    bool negate = false;
                    uint value;
                    if (vals[2].Equals("under", StringComparison.OrdinalIgnoreCase))
                    {
                        value = uint.Parse(vals[3]);
                        negate = true;
                    }
                    else
                    {
                        value = uint.Parse(vals[2]);
                    }
                    __result = Game1.getAllFarmers().Any((Farmer farmer) => StatsManager.GrabBasicProperty(statistic, farmer.stats) >= value);
                    if (negate) { __result = !__result; }
                    return false;
                }
                else if (__0.StartsWith("walnutcount_"))
                {
                    string [] vals = __0.Split('_');
                    if (vals[1].Equals("under", StringComparison.OrdinalIgnoreCase))
                    {
                        __result = (int)Game1.netWorldState.Value.GoldenWalnutsFound.Value < int.Parse(vals[2]);
                    }
                    else
                    {
                        __result = (int)Game1.netWorldState.Value.GoldenWalnutsFound.Value >= int.Parse(vals[1]);
                    }
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
                ModMonitor.Log(I18n.Get("load-save-first"), LogLevel.Debug);
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
                    ModMonitor.Log($"{tag}: {I18n.Get("true")}", LogLevel.Debug);
                }
                else
                {
                    ModMonitor.Log($"{tag}: {I18n.Get("false")}", LogLevel.Debug);
                }
            }
        }

        private void GetAvailableOrders(string command, string[] args)
        {
            if (!Context.IsWorldReady) { ModMonitor.Log(I18n.Get("load-save-first"), LogLevel.Warn); }
            Dictionary<string, SpecialOrderData> order_data = Game1.content.Load <Dictionary<string, SpecialOrderData>>("Data\\SpecialOrders");
            List<string> keys = new(order_data.Keys);
            ModMonitor.Log(I18n.Get("number-found").Tokens( new {count= keys.Count}), LogLevel.Debug);

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
            ModMonitor.Log($"{validkeys.Count} {I18n.Get("valid-keys")}: {string.Join(", ", validkeys)}", LogLevel.Debug);
            ModMonitor.Log($"{unseenkeys.Count} {I18n.Get("unsceen-keys")}: {string.Join(", ", unseenkeys)}", LogLevel.Debug);
        }

        private bool IsAvailableOrder(string key, SpecialOrderData order)
        {
            ModMonitor.Log($"{I18n.Get("analyzing")} {key}", LogLevel.Debug);
            try
            {
                SpecialOrder.GetSpecialOrder(key, Game1.random.Next());
                ModMonitor.Log($"    {key} {I18n.Get("parsable")}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"    {key} {I18n.Get("unparsable")}\n{ex}", LogLevel.Error);
                return false;
            }

            bool seen = Game1.MasterPlayer.team.completedSpecialOrders.ContainsKey(key);
            if (order.Repeatable != "True" && seen)
            {
                ModMonitor.Log($"    {I18n.Get("nonrepeatable")}", LogLevel.Debug);
                return false;
            }
            else if (seen)
            {
                ModMonitor.Log($"    {I18n.Get("repeatable-seen")}", LogLevel.Debug);
            }
            if (Game1.dayOfMonth >= 16 && order.Duration == "Month")
            {
                ModMonitor.Log($"    {I18n.Get("month-long-late").Tokens(new { cutoff = 16 })}");
                return false;
            }
            if (!SpecialOrder.CheckTags(order.RequiredTags))
            {
                ModMonitor.Log($"    {I18n.Get("has-invalid-tags")}:", LogLevel.Debug);
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
                        ModMonitor.Log($"         {I18n.Get("tag-failed")}: {tag}", LogLevel.Debug);
                    }
                }
                return false;
            }
            foreach (SpecialOrder specialOrder in Game1.player.team.specialOrders)
            {
                if (specialOrder.questKey.Value == key)
                {
                    ModMonitor.Log($"    {I18n.Get("active")}", LogLevel.Debug);
                    return false;
                }
            }
            return true;
        }
    }
}
