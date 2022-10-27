using AtraCore.Framework.Caches;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using StardewModdingAPI.Events;

using AtraUtils = AtraShared.Utils.Utils;

namespace SleepInWedding;

/// <inheritdoc />
[HarmonyPatch(typeof(GameLocation))]
internal sealed class ModEntry : Mod
{
    private const string RestoredWeddings = "RestoredWeddings";

    /// <summary>
    /// Gets the config for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <summary>
    /// Gets the logging instance for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModMonitor = this.Monitor;

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoad;
        helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        helper.Events.GameLoop.DayStarted += this.OnDayStart;

        helper.Events.Multiplayer.ModMessageReceived += this.OnMessageRecieved;

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    private void OnDayStart(object? sender, DayStartedEventArgs e)
    {
        if (Game1.player.HasWeddingToday() && NPCCache.GetByVillagerName(Game1.player.spouse) is NPC spouse)
        {
            if (spouse.Dialogue.ContainsKey("DayOfWedding"))
            {
                spouse.ClearAndPushDialogue("DayOfWedding");
            }
            else
            {
                spouse.CurrentDialogue.Clear();
                spouse.CurrentDialogue.Push(new(I18n.WeddingGreeting(), spouse));
            }
        }
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        if (Game1.weddingsToday.Count > 0)
        {
            if (e.NewTime == 610)
            {
                int hour = Math.DivRem(Config.WeddingTime, 100, out int minutes);
                if (Game1.player.HasWeddingToday())
                {
                    Game1.addHUDMessage(new HUDMessage(I18n.WeddingMessage(hour, minutes), HUDMessage.achievement_type));
                }
                else
                {
                    Game1.addHUDMessage(new HUDMessage(I18n.WeddingMessageOther(hour, minutes), HUDMessage.achievement_type));
                }
            }
            else if (Game1.timeOfDay == Config.WeddingTime - 10)
            {
                Game1.addHUDMessage(new HUDMessage(I18n.WeddingReminder(), HUDMessage.achievement_type));
            }
            else if (Game1.timeOfDay == Config.WeddingTime)
            {
                Game1.player.currentLocation.checkForEvents();
            }
        }
    }

    /// <summary>
    /// calls queueWeddingsForToday just after save is loaded.
    /// Game doesn't seem to call it.
    /// </summary>
    /// <param name="sender">SMAPI.</param>
    /// <param name="e">Event args.</param>
    private void OnSaveLoad(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            if (Game1.canHaveWeddingOnDay(Game1.dayOfMonth, Game1.currentSeason))
            {
                return;
            }

            ModMonitor.DebugOnlyLog($"Before attempting queuing new weddings {string.Join(", ", Game1.weddingsToday)}");

            HashSet<long> added = new();
            List<long> online = new();
            foreach (var farmer in Game1.getOnlineFarmers())
            {
                // we'll need a list of farmers to broadcast to....
                if (farmer.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
                {
                    online.Add(farmer.UniqueMultiplayerID);
                }

                if (farmer.spouse is not null && farmer.friendshipData.TryGetValue(farmer.spouse, out var friendship)
                    && friendship.CountdownToWedding == 1)
                {
                    if (added.Add(farmer.UniqueMultiplayerID))
                    {
                        Game1.weddingsToday.Add(farmer.UniqueMultiplayerID);
                    }
                }
                else if (!added.Contains(farmer.UniqueMultiplayerID))
                {
                    long? other = farmer.team.GetSpouse(farmer.UniqueMultiplayerID);
                    if (other is not null)
                    {
                        FarmerPair team = FarmerPair.MakePair(other.Value, farmer.UniqueMultiplayerID);
                        if (farmer.team.friendshipData.TryGetValue(team, out var farmerteam)
                            && farmerteam.CountdownToWedding == 1)
                        {
                            if (added.Add(farmer.UniqueMultiplayerID))
                            {
                                Game1.weddingsToday.Add(farmer.UniqueMultiplayerID);
                            }
                            if (added.Add(other.Value))
                            {
                                Game1.weddingsToday.Add(other.Value);
                            }
                        }
                    }
                }
            }

            ModMonitor.DebugOnlyLog($"Current weddings {string.Join(", ", Game1.weddingsToday)}");
            this.Helper.Multiplayer.SendMessage(
                message: Game1.weddingsToday,
                messageType: RestoredWeddings,
                modIDs: new[] { this.ModManifest.UniqueID },
                playerIDs: online.ToArray());
        }
    }

    private void OnMessageRecieved(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != this.ModManifest.UniqueID)
        {
            return;
        }
        if (e.Type == RestoredWeddings)
        {
            List<long>? weddings = e.ReadAs<List<long>>();
            if (weddings is not null)
            {
                Game1.weddingsToday.Clear();
                Game1.weddingsToday.AddRange(weddings);
            }
        }
    }

    /// <summary>
    /// Applies the patches for this mod.
    /// </summary>
    /// <param name="harmony">This mod's harmony instance.</param>
    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, this.ModManifest.UniqueID, transpilersOnly: true);
    }

    /// <summary>
    /// Sets up the GMCM for this mod.
    /// </summary>
    /// <param name="sender">SMAPI.</param>
    /// <param name="e">event args.</param>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (helper.TryGetAPI())
        {
            helper.Register(
                reset: static () => Config = new(),
                save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config))
            .GenerateDefaultGMCM(static () => Config);
        }
    }
}