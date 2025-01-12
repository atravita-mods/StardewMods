using HarmonyLib;

using MiniAtraShared.Models;

using RenderDisplayRewrite.Framework;
using RenderDisplayRewrite.HarmonyPatches;

using StardewModdingAPI.Events;

using xTile.Tiles;

namespace RenderDisplayRewrite;

/// <inheritdoc/>
internal class ModEntry : BaseMod<ModEntry>
{
    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        base.Entry(helper);

        helper.Events.Content.AssetReady += (_, e) => DisplayDeviceManager.Ready(e);
        helper.Events.Content.AssetsInvalidated += (_, e) => DisplayDeviceManager.Invalidate(e.Names);

        helper.Events.Player.Warped += this.OnWarp;

        Harmony harmony = new (this.ModManifest.UniqueID);

        ReplaceDisplayDevice.ApplyPatches(harmony);
        ReplaceStaticTile.Apply(harmony);
    }

    /// <inheritdoc cref="IPlayerEvents.Warped"/>
    private void OnWarp(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer || ReferenceEquals(e.NewLocation, e.OldLocation))
        {
            return;
        }

        if (Game1.mapDisplayDevice is DisplayDevice device)
        {
            device.ClearCache();
            foreach (TileSheet? tilesheet in e.NewLocation.Map.TileSheets)
            {
                device.TryLoadTilesheetTexture(tilesheet);
            }
        }

    }
}
