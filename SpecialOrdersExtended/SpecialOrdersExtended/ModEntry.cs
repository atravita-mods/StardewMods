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
        public static StatsManager StatsManager;
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
                prefix: new HarmonyMethod(typeof(TagManager), nameof(TagManager.PrefixCheckTag))
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

            helper.Events.GameLoop.GameLaunched += RegisterTokens;
            helper.Events.GameLoop.SaveLoaded += SaveLoaded;
            helper.Events.GameLoop.Saving += Saving;
        }

        private void RegisterTokens(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            var api = this.Helper.ModRegistry.GetApi<Tokens.IContentPatcherAPI>("Pathoschild.ContentPatcher");
            if (api == null) { ModMonitor.Log("Content Patcher not installed, tokens will not be available", LogLevel.Warn); return; }

            api.RegisterToken(this.ModManifest, "Current", new Tokens.CurrentSpecialOrders());
            api.RegisterToken(this.ModManifest, "Available", new Tokens.AvailableSpecialOrders());
            api.RegisterToken(this.ModManifest, "Completed", new Tokens.CompletedSpecialOrders());
        }

        private void Saving(object sender, StardewModdingAPI.Events.SavingEventArgs e)
        {
            StatsManager.ClearProperties();
            DialogueManager.SaveDialogueLog();
        }

        private void SaveLoaded(object sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        {
            DialogueManager.LoadDialogueLog();
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
