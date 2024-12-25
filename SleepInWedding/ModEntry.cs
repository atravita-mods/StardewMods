using AtraCore.Framework.Caches;
using AtraCore.Utilities;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces.ContentPatcher;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewModdingAPI.Events;
using MiniAtraShared.Extensions;
using MiniAtraShared.Models;

using AtraUtils = MiniAtraShared.Utils;

namespace SleepInWedding;

/// <inheritdoc />
[HarmonyPatch(typeof(GameLocation))]
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <summary>
    /// Gets the config for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        base.Entry(helper);
        I18n.Init(helper.Translation);

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoad;
        helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        helper.Events.GameLoop.DayStarted += this.OnDayStart;

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
    private void OnDayStart(object? sender, DayStartedEventArgs e)
    {
        if (Game1.player.HasWeddingToday() && Game1.player.spouse is not null && NPCCache.GetByVillagerName(Game1.player.spouse) is NPC spouse)
        {
            spouse.currentMarriageDialogue.Clear();

            if (spouse.Dialogue.ContainsKey("DayOfWedding"))
            {
                spouse.ClearAndPushDialogue("DayOfWedding");
            }
            else
            {
                spouse.CurrentDialogue.Clear();
                spouse.CurrentDialogue.Push(new(spouse, null, I18n.WeddingGreeting()));
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
                    Game1.addHUDMessage(new HUDMessage(I18n.WeddingMessage(hour, minutes.ToString("D2")), HUDMessage.achievement_type));
                }
                else
                {
                    Game1.addHUDMessage(new HUDMessage(I18n.WeddingMessageOther(hour, minutes.ToString("D2")), HUDMessage.achievement_type));
                }
            }
            else if (Game1.timeOfDay == Config.WeddingTime)
            {
                Game1.warpFarmer(new LocationRequest("Town", false, Game1.getLocationFromName("Town")), 5, 10, 0);
            }
            else if (Game1.timeOfDay == Utility.ModifyTime(Config.WeddingTime, -20))
            {
                Game1.addHUDMessage(new HUDMessage(I18n.WeddingReminder(), HUDMessage.achievement_type));
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
        if (Context.IsSplitScreen && Context.ScreenId != 0)
        {
            return;
        }

        MultiplayerHelpers.AssertMultiplayerVersions(this.Helper.Multiplayer, this.ModManifest, this.Monitor, this.Helper.Translation);
    }

    /// <summary>
    /// Applies the patches for this mod.
    /// </summary>
    /// <param name="harmony">This mod's harmony instance.</param>
    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll(typeof(ModEntry).Assembly);
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

        IntegrationHelper integrationHelper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry);
        if (integrationHelper.TryGetAPI("Pathoschild.ContentPatcher", "1.19.0", out IContentPatcherAPI? api))
        {
            api.RegisterToken(
                mod: this.ModManifest,
                name: "IsCurrentlyWedding",
                getValue: () => [(Game1.CurrentEvent is Event evt && evt.isWedding).ToString()]);
        }
    }
}