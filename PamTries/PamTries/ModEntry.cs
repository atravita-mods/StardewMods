using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.MigrationManager;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using Microsoft.Xna.Framework;
using PamTries.HarmonyPatches;
using StardewModdingAPI.Events;

namespace PamTries;

public enum PamMood
{
    bad,
    neutral,
    good,
}

/// <inheritdoc />
internal class ModEntry : Mod
{
    private static readonly string[] SyncedConversationTopics = new string[2] { "PamTriesRehab", "PamTriesRehabHoneymoon" };
    private Random? random;
    private PamMood mood = PamMood.neutral;
    private MigrationManager? migrator;

    // set in Entry, which is as close as I can get to the constructor
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// The logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);

        ModMonitor = this.Monitor;

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunch;
        helper.Events.GameLoop.DayStarted += this.DayStarted;
        helper.Events.GameLoop.SaveLoaded += this.SaveLoaded;
        helper.Events.GameLoop.DayStarted += DialogueManager.GrandKidsDialogue;
        helper.Events.GameLoop.DayEnding += this.DayEnd;

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
#if DEBUG
            BusDriverTranspile.ApplyPatch(harmony);
#endif
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            ModMonitor.Log($"Mod crashed while applying Harmony patches.\n\n{ex}", LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, uniqueID: harmony.Id, transpilersOnly: true);
    }

    private void DayStarted(object? sender, DayStartedEventArgs e)
        => PTUtilities.PopulateLexicon(this.Helper.GameContent);

    private void SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.SetPamMood((int)Game1.stats.DaysPlayed);
        PTUtilities.SyncConversationTopics(SyncedConversationTopics);
        PTUtilities.LocalEventSyncs(ModMonitor);

        if (Context.IsSplitScreen && Context.ScreenId != 0)
        {
            return;
        }

        PTUtilities.PopulateLexicon(this.Helper.GameContent);
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

    private void OnGameLaunch(object? sender, GameLaunchedEventArgs e)
    {
        IntegrationHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry);
        if (!helper.TryGetAPI("Pathoschild.ContentPatcher", "1.20.0", out IContentPatcherAPI? api))
        {
            return;
        }

        api.RegisterToken(this.ModManifest, "CurrentMovie", () =>
        {
            // save is loaded
            if (Context.IsWorldReady)
            {
                return new[] { DialogueManager.CurrentMovie() };
            }
            return null;
        });
        api.RegisterToken(this.ModManifest, "ChildrenCount", () =>
        {
            // save is loaded
            if (Context.IsWorldReady)
            {
                return new[] { DialogueManager.ChildCount() };
            }
            return null;
        });
        api.RegisterToken(this.ModManifest, "ListChildren", () =>
        {
            if (Context.IsWorldReady)
            {
                return new[] { DialogueManager.ListChildren() };
            }
            return null;
        });
        api.RegisterToken(this.ModManifest, "PamMood", () =>
        {
            if (Context.IsWorldReady)
            {
                return new[] { this.GetPamMood() };
            }
            return null;
        });
    }

    private string GetPamMood()
        => this.mood.ToString();

    private void SetPamMood(int daysPlayed)
    {
        this.random = new Random((int)Game1.uniqueIDForThisGame + (60 * daysPlayed));
        double[] moodchances = new double[2];
        if (Game1.getAllFarmers().Any((Farmer farmer) => farmer.activeDialogueEvents.ContainsKey("PamTriesRehabHoneymoon")))
        {// two weeks after return from rehab, always good mood.
            moodchances[0] = 0;
            moodchances[1] = 0;
        }
        else if (Game1.getCharacterFromName("Penny")?.getSpouse() is Farmer spouse
                 && spouse.friendshipData["Penny"].IsMarried()
                 && spouse.friendshipData["Penny"].Points <= 2000)
        {// marriage penalty
            moodchances[0] = 0.4;
            moodchances[1] = 0.8;
        }
        else if (Game1.MasterPlayer.mailReceived.Contains("atravita_PamApology_Reward"))
        {// finished the special order
            moodchances[0] = 0.1;
            moodchances[1] = 0.5;
        }
        else if (Game1.MasterPlayer.eventsSeen.Contains(99210002))
        {// rehab event
            moodchances[0] = 0.2;
            moodchances[1] = 0.6;
        }
        else
        {
            moodchances[0] = 0.3333;
            moodchances[1] = 0.6667;
        }

        double chance = this.random.NextDouble();
        if (chance < moodchances[0])
        {
            this.mood = PamMood.bad;
        }
        else if (chance < moodchances[1])
        {
            this.mood = PamMood.neutral;
        }
        else
        {
            this.mood = PamMood.good;
        }
    }

    /// <summary>
    /// Undoes the changes to Pam's sprite at the end of the day, in case player sleeps while Pam fishes. Also, implements rehab invisibility
    /// </summary>
    /// <param name="sender">SMAPI.</param>
    /// <param name="args">Day end argmuments.</param>
    private void DayEnd(object? sender, DayEndingEventArgs args)
    {
        // reset Pam's sprite
        NPC? pam = Game1.getCharacterFromName("Pam");
        pam.Sprite.SpriteHeight = 32;
        pam.Sprite.SpriteWidth = 16;
        pam.Sprite.ignoreSourceRectUpdates = false;
        pam.Sprite.UpdateSourceRect();
        pam.drawOffset.Value = Vector2.Zero;
        pam.IsInvisible = false;

        PTUtilities.SyncConversationTopics(SyncedConversationTopics);

        if (Game1.player.activeDialogueEvents.TryGetValue("PamTriesRehab", out int days) && days > 1)
        {
            ModMonitor.Log("Pam set to invisible for rehab", LogLevel.Debug);
            pam.daysUntilNotInvisible = 2;
        }
        // bad marriage penalty. Consider implementing divorce.
        if (Context.IsMainPlayer)
        {
            if (Game1.getCharacterFromName("Penny").getSpouse() is Farmer pennySpouse && pennySpouse.friendshipData["Penny"].Points <= 2000)
            {
                pennySpouse.changeFriendship(-50, pam);
                ModMonitor.Log("Bad marriage penalty, 50 friendship lost with Pam", LogLevel.Trace);
            }
        }

        PTUtilities.LocalEventSyncs(ModMonitor);
        // Setup tomorrow:
        this.SetPamMood((int)Game1.stats.DaysPlayed + 1);

        if (Game1.getAllFarmers().Any((Farmer farmer) => farmer.mailForTomorrow.Contains("atravita_PamTries_PennyThanks")))
        {
            Game1.addMailForTomorrow("atravita_PamTries_BusNotice");
        }

        // Ensure the master player is synced.
        if (!Context.IsMainPlayer)
        {
            return;
        }
        if (Game1.getAllFarmers().Any((Farmer farmer) => farmer.hasOrWillReceiveMail("atravita_PamTries_PennyThanks")))
        {
            foreach (Farmer farmer in Game1.getAllFarmers())
            {
                if (!farmer.eventsSeen.Contains(99210001))
                {
                    farmer.eventsSeen.Add(99210001);
                }
            }
        }
        if (Game1.getAllFarmers().Any((Farmer farmer) => farmer.eventsSeen.Contains(99210002)))
        {
            foreach (Farmer farmer in Game1.getAllFarmers())
            {
                if (!farmer.eventsSeen.Contains(99210002))
                {
                    farmer.eventsSeen.Add(99210002);
                }
            }
        }
    }
}
