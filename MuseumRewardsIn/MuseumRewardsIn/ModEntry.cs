namespace MuseumRewardsIn;

using AtraCore.Framework.Internal;

using AtraShared.Integrations;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

using StardewValley.Internal;
using StardewValley.Locations;

using AtraUtils = AtraShared.Utils.Utils;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <summary>
    /// String key used for the museum shop's item resolver.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Preference.")]
    internal const string MUSEUM_RESOLVER = "atravita_MUSEUM_SHOP";

    /// <summary>
    /// Gets the config class for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        base.Entry(helper);
        AssetManager.Initialize(helper.GameContent);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.Player.Warped += this.OnWarped;
        helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Apply(e);

        helper.Events.Content.AssetsInvalidated += static (_, e) => AssetManager.Invalidate(e.NamesWithoutLocale);

        helper.Events.Player.InventoryChanged += static (_, e) => LibraryMuseumPatches.OnInventoryChanged(e.Added);

        I18n.Init(helper.Translation);

        Harmony harmony = new(this.ModManifest.UniqueID);
        harmony.PatchAll(typeof(ModEntry).Assembly);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // Register custom item resolver
        ItemQueryResolver.Register(MUSEUM_RESOLVER, MuseumShopBuilder.MuseumQuery);

        // move the default one to the left for SVE.
        Vector2 shopLoc = this.Helper.ModRegistry.IsLoaded("FlashShifter.SVECode") ? new(3, 9) : new(4, 9);

        Config = AtraUtils.GetConfigOrDefault<ModConfig>(this.Helper, this.Monitor);
        if (Config.BoxLocation == new Vector2(-1, -1))
        {
            Config.BoxLocation = shopLoc;
            this.Helper.AsyncWriteConfig(this.Monitor, Config);
        }

        GMCMHelper gmcm = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (gmcm.TryGetAPI())
        {
            gmcm.Register(
                reset: static () => Config = new(),
                save: () =>
                {
                    this.Helper.GameContent.InvalidateCacheAndLocalized(AssetManager.LibraryHouse.BaseName);
                    this.Helper.AsyncWriteConfig(this.Monitor, Config);
                })
            .AddTextOption(
                name: I18n.BoxLocation_Name,
                getValue: static () => Config.BoxLocation.X + ", " + Config.BoxLocation.Y,
                setValue: (str) => Config.BoxLocation = str.TryParseVector2(out Vector2 vec) ? vec : shopLoc,
                tooltip: I18n.BoxLocation_Description)
            .AddBoolOption(
                name: I18n.AllowBuyBacks_Name,
                getValue: static () => Config.AllowBuyBacks,
                setValue: static (value) => Config.AllowBuyBacks = value,
                tooltip: I18n.AllowBuyBacks_Description);
        }
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer)
        {
            return;
        }
        if (e.NewLocation is LibraryMuseum)
        {
            void AddBox()
            {
                Vector2 tile = Config.BoxLocation;

                this.Monitor.DebugOnlyLog($"Adding box to {tile}", LogLevel.Info);

                // add box
                e.NewLocation.temporarySprites.Add(new TemporaryAnimatedSprite
                {
                    texture = Game1.mouseCursors2,
                    sourceRect = new Rectangle(129, 210, 13, 16),
                    animationLength = 1,
                    sourceRectStartingPos = new Vector2(129f, 210f),
                    interval = 50000f,
                    totalNumberOfLoops = 9999,
                    position = (new Vector2(tile.X, tile.Y - 1) * Game1.tileSize) + (new Vector2(3f, 0f) * Game1.pixelZoom),
                    scale = Game1.pixelZoom,
                    layerDepth = Math.Clamp((((tile.Y - 0.5f) * Game1.tileSize) / 10000f) + 0.01f, 0f, 1.0f), // a little offset so it doesn't show up on the floor.
                    id = 777,
                });
            }

            if (Game1.CurrentEvent is { } evt)
            {
                if (evt.onEventFinished is { } action)
                {
                    action += AddBox;
                }
                else
                {
                    evt.onEventFinished = AddBox;
                }
            }
            else
            {
                AddBox();
            }
        }
        else
        {
            AssetManager.Invalidate();
        }
    }
}
