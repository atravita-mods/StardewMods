using ContentPatcher;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace PamTries
{

    public enum PamMood
    {
        Bad,
        Neutral,
        Good,
    }

    public class ModEntry : Mod
    {

        private static IMonitor ModMonitor;
        private static IContentHelper ContentHelper;
        private Random random;
        private PamMood mood = PamMood.Neutral;

        public override void Entry(IModHelper helper)
        {
            Harmony harmony = new(this.ModManifest.UniqueID);
            ModMonitor = this.Monitor;
            ContentHelper = helper.Content;

            ModMonitor.LogOnce("Patching NPC:startRouteBehavior and NPC:finishRouteBehavior so Pam can fish", LogLevel.Debug);
            harmony.Patch(
                original: typeof(NPC).GetMethod("startRouteBehavior", BindingFlags.NonPublic | BindingFlags.Instance),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.StartFishBehavior))
                );

            harmony.Patch(
                original: typeof(NPC).GetMethod("finishRouteBehavior", BindingFlags.NonPublic | BindingFlags.Instance),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.EndFishBehavior))
                );
            
            helper.Events.GameLoop.GameLaunched += OnGameLaunch;
            helper.Events.GameLoop.DayStarted += DayStarted;
            helper.Events.GameLoop.SaveLoaded += SaveLoaded;
            helper.Events.GameLoop.SaveCreating += EnsureMasterSync;
            helper.Events.GameLoop.DayStarted += Dialogue.GrandKidsDialogue;
            helper.Events.GameLoop.DayEnding += DayEnd;
            //SButton MenuButton = Game1.options.menuButton[0].ToSButton();
        }

        private void DayStarted(object sender, DayStartedEventArgs e)
        {//grab Lexicon anew each day.
            PTUtilities.PopulateLexicon(ContentHelper);
        }

        public void EnsureMasterSync(object sender, SaveCreatingEventArgs e)
        {
            if (!Game1.IsMasterGame) { return; }
            if (Game1.getAllFarmers().Any((Farmer farmer) => farmer.hasOrWillReceiveMail("atravita_PamTries_PennyThanks")))
            {
                foreach (Farmer farmer in Game1.getAllFarmers())
                {
                    if (!farmer.eventsSeen.Contains(99210001)) { farmer.eventsSeen.Add(99210001); }
                }
            }
            if (Game1.getAllFarmers().Any((Farmer farmer) => farmer.eventsSeen.Contains(99210002)))
            {
                foreach (Farmer farmer in Game1.getAllFarmers())
                {
                    if (!farmer.eventsSeen.Contains(99210002)) { farmer.eventsSeen.Add(99210002); }
                }
            }
        }

        private void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            SetPamMood((int)Game1.stats.DaysPlayed);
            PTUtilities.SyncConversationTopics("PamTriesRehab");
            PTUtilities.SyncConversationTopics("PamTriesRehabHoneymoon");
            PTUtilities.LocalEventSyncs(ModMonitor);
            PTUtilities.PopulateLexicon(ContentHelper);
        }

        private void OnGameLaunch(object sender, GameLaunchedEventArgs e)
        {
            var CPapi = this.Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");
            CPapi.RegisterToken(this.ModManifest, "CurrentMovie", () =>
            {
                // save is loaded
                if (Context.IsWorldReady) { return new[] { Dialogue.CurrentMovie() }; }
                return null;
            });
            CPapi.RegisterToken(this.ModManifest, "ChildrenCount", () =>
            {
                // save is loaded
                if (Context.IsWorldReady) { return new[] { Dialogue.ChildCount() }; }
                return null;
            });
            CPapi.RegisterToken(this.ModManifest, "ListChildren", () =>
            {
                if (Context.IsWorldReady) { return new[] { Dialogue.ListChildren() }; }
                return null;
            });
            CPapi.RegisterToken(this.ModManifest, "PamMood", () =>
            {
                if (Context.IsWorldReady) { return new[] { GetPamMood() }; }
                return null;
            });
            ModMonitor.Log("Tokens loaded", LogLevel.Trace);
        }

        private string GetPamMood()
        {
            return mood switch
            {
                (PamMood.Bad) => "bad",
                (PamMood.Neutral) => "neutral",
                (PamMood.Good) => "good",
                _ => "neutral",
            };
        }

        private void SetPamMood(int DaysPlayed)
        {
            this.random = new Random((int)Game1.uniqueIDForThisGame + (60 * DaysPlayed));
            NPC Penny = Game1.getCharacterFromName("Penny");
            Farmer spouse = Penny?.getSpouse();
            double[] moodchances = new double[2];
            if (Game1.getAllFarmers().Any((Farmer farmer)=> farmer.activeDialogueEvents.ContainsKey("PamTriesRehabHoneymoon")))
            {//two weeks after return from rehab, always good mood.
                moodchances[0] = 0; moodchances[1] = 0;
            }
            else if (spouse != null && spouse.friendshipData["Penny"].IsMarried() && spouse.friendshipData["Penny"].Points <= 2000)
            {//marriage penalty
                moodchances[0] = 0.4; moodchances[1] = 0.8;
            }
            else if (Game1.MasterPlayer.mailReceived.Contains("atravita_PamApology_Reward"))
            {//finished the special order
                moodchances[0] = 0.1; moodchances[1] = 0.5;
            }
            else if (Game1.MasterPlayer.eventsSeen.Contains(99210002)) //rehab event
            {
                moodchances[0] = 0.2; moodchances[1] = 0.6;
            }
            else
            {
                moodchances[0] = 0.3333; moodchances[1] = 0.6667;
            }
            double chance = random.NextDouble();
            if (chance < moodchances[0]) { this.mood = PamMood.Bad; }
            else if (chance < moodchances[1]) { this.mood = PamMood.Neutral; }
            else { this.mood = PamMood.Good; }
        }

        private static void StartFishBehavior(NPC __instance, ref string __0)
        {
            try
            {
                if (__0 == "pam_fish")
                {
                    __instance.extendSourceRect(0, 32);
                    __instance.Sprite.tempSpriteHeight = 64;
                    __instance.drawOffset.Value = new Vector2(0f, 96f);
                    __instance.Sprite.ignoreSourceRectUpdates = false;
                    if (Utility.isOnScreen(Utility.Vector2ToPoint(__instance.Position), 64, __instance.currentLocation))
                    {
                        __instance.currentLocation.playSoundAt("slosh", __instance.getTileLocation());
                    }
                }
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"Failed to adjust startRouteBehavior for Pam\n{ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Reset Pam's fishing sprite when done fishing
        /// </summary>
        /// <param name="__instance">NPC</param>
        /// <param name="__0">animation_description</param>
        private static void EndFishBehavior(NPC __instance, ref string __0)
        {
            try
            {
                if (__0 == "pam_fish")
                {
                    __instance.reloadSprite();
                    __instance.Sprite.SpriteWidth = 16;
                    __instance.Sprite.SpriteHeight = 32;
                    __instance.Sprite.UpdateSourceRect();
                    __instance.drawOffset.Value = Vector2.Zero;
                    __instance.Halt();
                    __instance.movementPause = 1;
                }
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"Failed to adjust finishRouteBehavior for Pam\n{ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Undoes the changes to Pam's sprite at the end of the day, in case player sleeps while Pam fishes. Also, implements rehab invisibility
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void DayEnd(object sender, DayEndingEventArgs args)
        {//reset Pam's sprite
            NPC Pam = Game1.getCharacterFromName("Pam");
            Pam.Sprite.SpriteHeight = 32;
            Pam.Sprite.SpriteWidth = 16;
            Pam.Sprite.ignoreSourceRectUpdates = false;
            Pam.Sprite.UpdateSourceRect();
            Pam.drawOffset.Value = Vector2.Zero;
            Pam.IsInvisible = false;

            PTUtilities.SyncConversationTopics("PamTriesRehab");
            PTUtilities.SyncConversationTopics("PamTriesRehabHoneymoon");
            if (Game1.player.activeDialogueEvents.ContainsKey("PamTriesRehab") && Game1.player.activeDialogueEvents["PamTriesRehab"] > 1)
            {
                ModMonitor.Log("Pam set to invisible for rehab", LogLevel.Debug);
                Pam.daysUntilNotInvisible = 2;
            }
            // bad marriage penalty. Consider implementing divorce.
            if (Game1.IsMasterGame)
            {
                Farmer PennySpouse = Game1.getCharacterFromName("Penny").getSpouse();
                if (PennySpouse != null && PennySpouse.friendshipData["Penny"].Points <= 2000)
                {
                    PennySpouse.changeFriendship(-50, Pam);
                    ModMonitor.Log("Bad marriage penalty, 50 friendship lost with Pam", LogLevel.Trace);
                }
            }

            PTUtilities.LocalEventSyncs(ModMonitor);
            //Setup tomorrow:
            SetPamMood((int)Game1.stats.DaysPlayed + 1);

            if (Game1.getAllFarmers().Any((Farmer farmer) => farmer.mailForTomorrow.Contains("atravita_PamTries_PennyThanks")))
            {
                Game1.addMailForTomorrow("atravita_PamTries_BusNotice");
            }
        }
    }
}
