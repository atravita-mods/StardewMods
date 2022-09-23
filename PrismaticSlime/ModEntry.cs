using AtraCore.Framework.IntegrationManagers;
using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using PrismaticSlime.Framework;
using StardewModdingAPI.Events;

namespace PrismaticSlime;

/// <inheritdoc/>
internal sealed class ModEntry : Mod
{
    /// <summary>
    /// String key used to index the number of slime balls popped.
    /// </summary>
    internal const string SlimePoppedStat = "atravita.SlimeBallsPopped";

    private static IJsonAssetsAPI? jsonAssets;

    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

#pragma warning disable SA1201 // Elements should appear in the correct order - keeping fields near their accessors.
    internal static RingManager RingManager { get; private set; } = null!;

    private static int prismaticSlimeEgg = -1;

    /// <summary>
    /// Gets the integer ID of the Prismatic Slime Egg. -1 if not found/not loaded yet.
    /// </summary>
    internal static int PrismaticSlimeEgg
    {
        get
        {
            if (prismaticSlimeEgg == -1)
            {
                prismaticSlimeEgg = jsonAssets?.GetObjectId("atravita.PrismaticSlime Egg") ?? -1;
            }
            return prismaticSlimeEgg;
        }
    }

    private static int prismaticSlimeRing = -1;

    /// <summary>
    /// Gets the integer ID of the Prismatic Slime Ring. -1 if not found/not loaded yet.
    /// </summary>
    internal static int PrismaticSlimeRing
    {
        get
        {
            if (prismaticSlimeRing == -1)
            {
                prismaticSlimeRing = jsonAssets?.GetObjectId("atravita.PrismaticSlimeRing") ?? -1;
            }
            return prismaticSlimeRing;
        }
    }
#pragma warning restore SA1201 // Elements should appear in the correct order

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;
        I18n.Init(helper.Translation);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        {
            IntegrationHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, LogLevel.Warn);
            if (!helper.TryGetAPI("spacechase0.JsonAssets", "1.10.3", out jsonAssets))
            {
                this.Monitor.Log("Packs could not be loaded! This mod will probably not function.", LogLevel.Error);
                return;
            }
            jsonAssets.LoadAssets(Path.Combine(this.Helper.DirectoryPath, "assets", "json-assets"), this.Helper.Translation);
        }

        RingManager = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry);

        this.Helper.Events.Content.AssetRequested += this.OnAssetRequested;
        this.Helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
    }

    /// <summary>
    /// Resets the IDs when returning to the title.
    /// </summary>
    /// <param name="sender">SMAPI.</param>
    /// <param name="e">Event args.</param>
    [EventPriority(EventPriority.High)]
    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        prismaticSlimeRing = -1;
        prismaticSlimeEgg = -1;
    }

    [EventPriority(EventPriority.Low)]
    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        => AssetManager.Apply(e);

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            // handle patches from annotations.
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }

        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }
}
