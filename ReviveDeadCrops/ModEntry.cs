using AtraShared.ConstantsAndEnums;
using AtraShared.Menuing;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using ReviveDeadCrops.Framework;
using StardewModdingAPI.Events;

namespace ReviveDeadCrops;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    internal static IMonitor ModMonitor { get; private set; } = null!;

    internal static ReviveDeadCropsApi Api { get; private set; } = new();

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    /// <inheritdoc />
    public override object? GetApi() => Api;

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

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!MenuingExtensions.IsNormalGameplay() || !(e.Button.IsUseToolButton() || e.Button.IsActionButton()))
        {
            return;
        }

        if (Game1.player.ActiveObject is SObject obj && Api.TryApplyDust(Game1.currentLocation, e.Cursor.GrabTile, obj))
        {
            this.Helper.Input.Suppress(e.Button);
            Api.AnimateRevival(Game1.currentLocation, e.Cursor.GrabTile);
            Game1.player.reduceActiveItemByOne();
        }
    }
}
