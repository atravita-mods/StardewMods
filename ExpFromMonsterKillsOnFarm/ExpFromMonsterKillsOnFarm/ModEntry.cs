using HarmonyLib;

using MiniAtraShared.Extensions;
using MiniAtraShared.Models;

using StardewModdingAPI.Events;

using StardewValley.Quests;
using StardewValley.SpecialOrders.Objectives;

using AtraUtils = MiniAtraShared.Utils;

namespace ExpFromMonsterKillsOnFarm;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <summary>
    /// Gets or sets an instance of the configuration class for this mod.
    /// </summary>
    internal static ModConfig Config { get; set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        base.Entry(helper);

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);
        ApplyPatches(new Harmony(this.ModManifest.UniqueID));
        helper.Events.GameLoop.GameLaunched += this.SetUpConfig;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;

        AssetEditor.Init(this.Helper.GameContent);
        helper.Events.Content.AssetRequested += static (_, e) => AssetEditor.Apply(e);
    }

    /// <summary>
    /// Applies this mod's harmony patches.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    private static void ApplyPatches(Harmony harmony) =>
        GameLocationPatches.ApplyPatch(harmony);

    private static void ApplyQuestSettings(Farmer farmer)
    {
        if (Config.QuestCompletion)
        {
            foreach (var quest in farmer.questLog)
            {
                if (quest is SlayMonsterQuest slay)
                {
                    slay.ignoreFarmMonsters.Value = false;
                }
            }
        }

        if (Config.SpecialOrderCompletion)
        {
            foreach (var order in Enumerable.Concat(Game1.player.team.specialOrders, Game1.player.team.availableSpecialOrders))
            {
                foreach (var objective in order.objectives)
                {
                    if (objective is SlayObjective slayObjective)
                    {
                        slayObjective.ignoreFarmMonsters.Value = false;
                    }
                }
            }
        }
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        ApplyQuestSettings(Game1.player);
        PlayerQuestEditor tracker = new PlayerQuestEditor();

        Game1.player.questLog.OnArrayReplaced += tracker.OnArrayReplaced;
        Game1.player.questLog.OnElementChanged += tracker.OnElementChanged;
    }


    /// <summary>
    /// Generates the GMCM for this mod by looking at the structure of the config class.
    /// </summary>
    /// <param name="sender">Unknown, expected by SMAPI.</param>
    /// <param name="e">Arguments for event.</param>
    private void SetUpConfig(object? sender, GameLaunchedEventArgs e)
    {
        const string GMCM = "spacechase0.GenericModConfigMenu";
        if (this.Helper.ModRegistry.Get(GMCM) is not { } gmcm || gmcm.Manifest.Version.IsOlderThan("1.9.0"))
        {
            return;
        }

        var api = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api is null)
        {
            return;
        }

        api.Register(
            this.ModManifest,
            reset: static () => Config = new(),
            save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config));

        api.AddParagraph(
            this.ModManifest,
            I18n.Mod_Description);

        api.AddBoolOption(
            this.ModManifest,
            static () => Config.GainExp,
            static value => Config.GainExp = value,
            I18n.GainExp_Title,
            I18n.GainExp_Description);

        api.AddBoolOption(
            this.ModManifest,
            static () => Config.QuestCompletion,
            static value => Config.QuestCompletion = value,
            I18n.QuestCompletion_Title,
            I18n.QuestCompletion_Description);

        api.AddBoolOption(
            this.ModManifest,
            static () => Config.SpecialOrderCompletion,
            static value => Config.SpecialOrderCompletion = value,
            I18n.SpecialOrderCompletion_Title,
            I18n.SpecialOrderCompletion_Description);
    }
}
