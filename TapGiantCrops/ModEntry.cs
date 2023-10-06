// Ignore Spelling: Api

using AtraCore.Framework.Internal;

using AtraShared.ConstantsAndEnums;
using AtraShared.Menuing;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

using StardewValley.TerrainFeatures;

using TapGiantCrops.Framework;

namespace TapGiantCrops;

/// <inheritdoc />
[HarmonyPatch(typeof(Utility))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal sealed class ModEntry : BaseMod<ModEntry>
{
    private static readonly TapGiantCrop Api = new();

    /// <summary>
    /// Gets the game content helper.
    /// </summary>
    internal static IGameContentHelper GameContent { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        AssetManager.Initialize(helper.GameContent);
        base.Entry(helper);

        GameContent = helper.GameContent;

        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.GameLoop.DayEnding += this.OnDayEnding;

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;

        helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Load(e);
        helper.Events.Content.AssetsInvalidated += static (_, e) => AssetManager.Reset(e.NamesWithoutLocale);

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    /// <inheritdoc />
    public override object? GetApi() => Api;

    /// <inheritdoc cref="IGameLoopEvents.DayEnding"/>
    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        Utility.ForEachLocation(static (location) =>
        {
            if (location?.resourceClumps is null)
            {
                return true;
            }

            foreach (ResourceClump? feature in location.resourceClumps)
            {
                if (feature is GiantCrop crop)
                {
                    Vector2 offset = crop.Tile;
                    offset.X += crop.width.Value / 2;
                    offset.Y += crop.height.Value - 1;
                    if (location.objects.TryGetValue(offset, out SObject? tapper) && tapper.IsTapper()
                        && tapper.heldObject is not null && tapper.heldObject.Value is null)
                    {
                        (SObject obj, int days)? output = Api.GetTapperProduct(crop, tapper);
                        if (output is not null)
                        {
                            tapper.heldObject.Value = output.Value.obj;
                            int days = output.Value.days;
                            tapper.MinutesUntilReady = Utility.CalculateMinutesUntilMorning(Game1.timeOfDay, Math.Max(1, days));
                            ModMonitor.DebugOnlyLog($"Assigning product to tapper at {location.NameOrUniqueName} {offset}", LogLevel.Info);
                        }
                    }
                }
            }
            return true;
        });
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

    /// <inheritdoc cref="IInputEvents.ButtonPressed"/>
    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!(e.Button.IsUseToolButton() || e.Button.IsActionButton())
            || !MenuingExtensions.IsNormalGameplay())
        {
            return;
        }
        if (Game1.player.ActiveObject is SObject obj && Api.TryPlaceTapper(Game1.currentLocation, e.Cursor.GrabTile, obj))
        {
            Game1.player.reduceActiveItemByOne();
        }
    }

    /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) => Api.Init();

    [HarmonyPriority(Priority.High)]
    [HarmonyPatch(nameof(Utility.playerCanPlaceItemHere))]
    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Reviewed.")]
    private static bool Prefix(GameLocation location, Item item, int x, int y, Farmer f, ref bool __result)
    {
        try
        {
            if (Utility.withinRadiusOfPlayer(x, y, 2, f) && item is SObject obj)
            {
                Vector2 tile = new(MathF.Floor(x / Game1.tileSize), MathF.Floor(y / Game1.tileSize));
                if (Api.CanPlaceTapper(location, tile, obj))
                {
                    __result = true;
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            ModMonitor.LogError("adding tapper to Utility.playerCanPlaceItemHere", ex);
        }
        return true;
    }
}
