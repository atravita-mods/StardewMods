// Ignore Spelling: Api

using AtraCore.Framework.Internal;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Utils.Extensions;

using ExperimentalLagReduction.Framework;
using ExperimentalLagReduction.HarmonyPatches;

using HarmonyLib;
using StardewModdingAPI.Events;

using StardewValley.Pathfinding;

using AtraUtils = AtraShared.Utils.Utils;

namespace ExperimentalLagReduction;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <summary>
    /// Gets the config instance for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // initialize translations.
        I18n.Init(helper.Translation);
        base.Entry(helper);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunch;
        helper.Events.GameLoop.DayEnding += this.CrosscheckCache;

        helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        OverrideGiftTastes.Initialize(helper.GameContent);
        ConsoleCommandManager.Register(helper.ConsoleCommands);

        AssetManager.Initialize(helper.GameContent);
        helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Apply(e);

        this.Monitor.Log($"We seem to have been allowed {Environment.ProcessorCount} processors.", LogLevel.Debug);
    }

    /// <inheritdoc />
    public override object? GetApi() => new API();

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        foreach (NPC? npc in Game1.currentLocation.characters.Concat((Game1.CurrentEvent?.actors as IEnumerable<NPC>) ?? Array.Empty<NPC>()))
        {
            if (npc.IsVillager && npc.AllowDynamicAppearance && !npc.SimpleNonVillagerNPC)
            {
                int? prevX = npc.Sprite?.SpriteWidth;
                int? prevY = npc.Sprite?.SpriteHeight;
                npc.ChooseAppearance();

                if (npc.Sprite is not null)
                {
                    if (prevX is not null)
                    {
                        npc.Sprite.SpriteWidth = prevX.Value;
                    }

                    if (prevY is not null)
                    {
                        npc.Sprite.SpriteHeight = prevY.Value;
                    }
                }
            }
        }
    }

    /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
    private void OnGameLaunch(object? sender, GameLaunchedEventArgs e)
    {
        this.Helper.Events.Content.AssetsInvalidated += (_, e) => OverrideGiftTastes.Reset(e.NamesWithoutLocale);
        this.Helper.Events.GameLoop.DayEnding += static (_, _) => OverrideGiftTastes.Reset();

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

        WarpPathfindingCache.IgnoreLocationNames.Add("VolcanoEntrance");

        // GMCM
        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (helper.TryGetAPI())
        {
            helper.Register(
                reset: () => Config = new(),
                save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config))
            .GenerateDefaultGMCM(() => Config);
        }
    }

    /// <summary>
    /// Applies the patches for this mod.
    /// </summary>
    /// <param name="harmony">This mod's harmony instance.</param>
    /// <remarks>Delay until GameLaunched in order to patch other mods....</remarks>
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
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    [EventPriority(EventPriority.Low - 250)]
    private void CrosscheckCache(object? sender, DayEndingEventArgs e)
    {
        Rescheduler.ClearNulls();
        Rescheduler.PrePopulateCache(false);
    }
}
