using AtraCore.Framework.Internal;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Integrations.Interfaces;
using AtraShared.MigrationManager;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using PrismaticSlime.Framework;

using StardewModdingAPI.Events;

namespace PrismaticSlime;

/// <inheritdoc/>
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <summary>
    /// String key used to index the number of slime balls popped.
    /// </summary>
    internal const string SlimePoppedStat = "atravita.SlimeBallsPopped";

    /// <summary>
    /// Int Id of the prismatic jelly.
    /// </summary>
    internal const string PrismaticJelly = "(O)876";

    private const string SAVEKEY = "item_ids";

    private static IJsonAssetsAPI? jsonAssets;
    private MigrationManager? migrator;

#pragma warning disable SA1201 // Elements should appear in the correct order - keeping fields near their accessors.
    private static int prismaticSlimeEgg = -1;

    /// <summary>
    /// Gets the integer BuffId of the Prismatic Slime Egg. -1 if not found/not loaded yet.
    /// </summary>
    internal const string PrismaticSlimeEgg = "atravita.PrismaticSlimeEgg";

    /// <summary>
    /// Gets the integer BuffId of the Prismatic Slime Ring. -1 if not found/not loaded yet.
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

    /// <summary>
    /// Gets the string id of the Prismatic Jelly Toast.
    /// </summary>
    internal const string PrismaticJellyToast = "atravita.PrismaticJellyToast";
#pragma warning restore SA1201 // Elements should appear in the correct order

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        base.Entry(helper);
        AssetManager.Initialize(helper.GameContent);
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

        this.Helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Apply(e);
        this.Helper.Events.Content.AssetsInvalidated += static (_, e) => AssetManager.Reset(e.NamesWithoutLocale);

        this.Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
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

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            // handle patches from annotations.
            harmony.PatchAll(typeof(ModEntry).Assembly);
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }

        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    #region migration

    /// <inheritdoc cref="IGameLoopEvents.Saved"/>
    /// <remarks>
    /// Writes migration data then detaches the migrator.
    /// </remarks>
    private void WriteMigrationData(object? sender, SavedEventArgs e)
    {
        if (this.migrator is not null)
        {
            this.migrator.SaveVersionInfo();
            this.migrator = null;
        }

        this.Helper.Events.GameLoop.Saved -= this.WriteMigrationData;
    }
    #endregion
}
