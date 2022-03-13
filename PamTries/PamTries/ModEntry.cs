using AtraBase.Toolkit.Reflection;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.MigrationManager;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;

namespace PamTries;

public enum PamMood
{
    Bad,
    Neutral,
    Good,
}

/// <inheritdoc />
public class ModEntry : Mod
{
    private static readonly string[] SyncedConversationTopics = new string[2] { "PamTriesRehab", "PamTriesRehabHoneymoon" };
    private Random? random;
    private PamMood mood = PamMood.Neutral;
    private MigrationManager? migrator;

    // set in Entry, which is as close as I can get to the constructor
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    internal static IMonitor ModMonitor { get; private set; }
    internal static IContentHelper ContentHelper { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;
        ContentHelper = helper.Content;

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunch;
        helper.Events.GameLoop.DayStarted += this.DayStarted;
        helper.Events.GameLoop.SaveLoaded += this.SaveLoaded;
        helper.Events.GameLoop.DayStarted += Dialogue.GrandKidsDialogue;
        helper.Events.GameLoop.DayEnding += this.DayEnd;

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            ModMonitor.Log($"Mod crashed while applying Harmony patches.\n\n{ex}", LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, this.ModManifest.UniqueID);
    }

    private void DayStarted(object? sender, DayStartedEventArgs e)
        => PTUtilities.PopulateLexicon(ContentHelper);

    private void SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.SetPamMood((int)Game1.stats.DaysPlayed);
        PTUtilities.SyncConversationTopics(SyncedConversationTopics);
        PTUtilities.LocalEventSyncs(ModMonitor);
        PTUtilities.PopulateLexicon(ContentHelper);

        if (Context.IsSplitScreen && Context.ScreenId != 0)
        {
            return;
        }
        this.migrator = new(this.ModManifest, this.Helper, this.Monitor);
        this.migrator.ReadVersionInfo();

        this.Helper.Events.GameLoop.Saved += this.WriteMigrationData;
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
                return new[] { Dialogue.CurrentMovie() };
            }
            return null;
        });
        api.RegisterToken(this.ModManifest, "ChildrenCount", () =>
        {
            // save is loaded
            if (Context.IsWorldReady)
            {
                return new[] { Dialogue.ChildCount() };
            }
            return null;
        });
        api.RegisterToken(this.ModManifest, "ListChildren", () =>
        {
            if (Context.IsWorldReady)
            {
                return new[] { Dialogue.ListChildren() };
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
        => this.mood.ToString().ToLowerInvariant();

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
            this.mood = PamMood.Bad;
        }
        else if (chance < moodchances[1])
        {
            this.mood = PamMood.Neutral;
        }
        else
        {
            this.mood = PamMood.Good;
        }
    }

    /// <summary>
    /// Undoes the changes to Pam's sprite at the end of the day, in case player sleeps while Pam fishes. Also, implements rehab invisibility
    /// </summary>
    /// <param name="sender">SMAPI.</param>
    /// <param name="args">Day end argmuments.</param>
    private void DayEnd(object? sender, DayEndingEventArgs args)
    {//reset Pam's sprite
        NPC? Pam = Game1.getCharacterFromName("Pam");
        Pam.Sprite.SpriteHeight = 32;
        Pam.Sprite.SpriteWidth = 16;
        Pam.Sprite.ignoreSourceRectUpdates = false;
        Pam.Sprite.UpdateSourceRect();
        Pam.drawOffset.Value = Vector2.Zero;
        Pam.IsInvisible = false;

        PTUtilities.SyncConversationTopics(SyncedConversationTopics);

        if (Game1.player.activeDialogueEvents.ContainsKey("PamTriesRehab") && Game1.player.activeDialogueEvents["PamTriesRehab"] > 1)
        {
            ModMonitor.Log("Pam set to invisible for rehab", LogLevel.Debug);
            Pam.daysUntilNotInvisible = 2;
        }
        // bad marriage penalty. Consider implementing divorce.
        if (Context.IsMainPlayer)
        {
            if (Game1.getCharacterFromName("Penny").getSpouse() is Farmer pennySpouse && pennySpouse.friendshipData["Penny"].Points <= 2000)
            {
                pennySpouse.changeFriendship(-50, Pam);
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
