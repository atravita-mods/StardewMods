// Ignore Spelling: Api

using AtraCore.Framework.Internal;

using AtraShared.ConstantsAndEnums;
using AtraShared.Menuing;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using ReviveDeadCrops.Framework;

using StardewModdingAPI.Events;

using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace ReviveDeadCrops;

/// <inheritdoc />
internal sealed class ModEntry : BaseMod<ModEntry>
{
    /// <summary>
    /// Gets the id of the everlasting fertilizer.
    /// </summary>
    internal const string EverlastingID = "atravita.EverlastingFertilizer";

    /// <summary>
    /// Gets the API for this mod.
    /// </summary>
    internal static ReviveDeadCropsApi Api { get; } = ReviveDeadCropsApi.Instance;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        base.Entry(helper);
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.GameLoop.DayEnding += this.OnDayEnd;

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    /// <inheritdoc />
    [UsedImplicitly]
    public override object? GetApi() => Api;

    // we need to make sure to slot in before Solid Foundations removes its buildings.
    [EventPriority(EventPriority.High + 20)]
    private void OnDayEnd(object? sender, DayEndingEventArgs e)
    {
        if (!Api.Changed)
        {
            return;
        }
        Api.Changed = false;

        Utility.ForEachLocation(
            action: (location) =>
            {
                if (location is null)
                {
                    return true;
                }

                foreach (TerrainFeature terrain in location.terrainFeatures.Values)
                {
                    if (terrain is HoeDirt dirt && dirt.modData?.GetBool(ReviveDeadCropsApi.REVIVED_PLANT_MARKER) == true
                        && dirt.crop is not null && dirt.fertilizer.Value != EverlastingID)
                    {
                        this.Monitor.DebugOnlyLog($"Found dirt with marker at {dirt.Tile} with crop {dirt.crop?.indexOfHarvest ?? "null"}");
                        dirt.modData?.SetBool(ReviveDeadCropsApi.REVIVED_PLANT_MARKER, false, false);
                        dirt.crop?.Kill();
                    }
                }

                foreach (SObject obj in location.Objects.Values)
                {
                    if (obj is IndoorPot pot && pot.hoeDirt.Value is HoeDirt dirt
                        && dirt.modData?.GetBool(ReviveDeadCropsApi.REVIVED_PLANT_MARKER) == true
                        && dirt.crop is not null && dirt.fertilizer.Value != EverlastingID)
                    {
                        this.Monitor.DebugOnlyLog($"Found dirt with marker at {dirt.Tile} with crop {dirt.crop?.indexOfHarvest ?? "null"}");
                        dirt.modData?.SetBool(ReviveDeadCropsApi.REVIVED_PLANT_MARKER, false, false);
                        dirt.crop?.Kill();
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

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!(e.Button.IsUseToolButton() || e.Button.IsActionButton())
            || !MenuingExtensions.IsNormalGameplay())
        {
            return;
        }

        if (Game1.player.ActiveObject is SObject obj && Api.TryApplyDust(Game1.currentLocation, e.Cursor.GrabTile, obj))
        {
            this.Helper.Input.Suppress(e.Button);
            Game1.player.reduceActiveItemByOne();
        }
    }
}
