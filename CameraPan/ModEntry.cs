using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Utils.Extensions;

using CameraPan.Framework;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

using AtraUtils = AtraShared.Utils.Utils;

namespace CameraPan;

// TODO: just re-write the viewport center code at this point.
// TODO: draw a big arrow pointing towards the player if the player is off screen?

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    /// <summary>
    /// The integer ID of the camera item.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
    internal const int CAMERA_ID = 106;

    private static readonly PerScreen<int> xOffset = new(() => 0);
    private static readonly PerScreen<int> yOffset = new(() => 0);

    internal static int XOffset => xOffset.Value;
    internal static int YOffset => yOffset.Value;

    /// <summary>
    /// Gets the config instance for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <summary>
    /// Gets the logging instance for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModMonitor = this.Monitor;
        Config = AtraUtils.GetConfigOrDefault<ModConfig>(helper, this.Monitor);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        helper.Events.GameLoop.UpdateTicked += this.OnTicked;
        helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;

        helper.Events.Player.Warped += this.OnWarped;
        helper.Events.Display.MenuChanged += this.OnMenuChanged;

        helper.Events.Display.WindowResized += static (_, _) => Config?.RecalculateBounds();
    }

    private static void Reset()
    {

        ModEntry.ModMonitor.Log("Reset!", LogLevel.Alert);
        xOffset.Value = 0;
        yOffset.Value = 0;
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (Config.ResetButton.JustPressed())
        {
            Reset();
        }
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
        Config.RecalculateBounds();

        GMCMHelper gmcmHelper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (gmcmHelper.TryGetAPI())
        {
            gmcmHelper.Register(
                reset: static () => Config = new(),
                save: () =>
                {
                    this.Helper.AsyncWriteConfig(this.Monitor, Config);
                    Config.RecalculateBounds();
                })
            .AddParagraph(I18n.Mod_Description)
            .GenerateDefaultGMCM(static () => Config);
        }
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.OldMenu is not null && e.NewMenu is null)
        {
            Reset();
            Game1.viewportTarget = new Vector2(-2.14748365E+09f, -2.14748365E+09f);
        }
        else if (e.OldMenu is null && e.NewMenu is not null)
        {
            Game1.moveViewportTo(Game1.player.Position, Config.Speed);
        }
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (e.IsLocalPlayer)
        {
            Reset();
            Game1.viewportTarget = new Vector2(-2.14748365E+09f, -2.14748365E+09f);
        }
    }

    private void OnTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsPlayerFree)
        {
            return;
        }
        Vector2 pos = this.Helper.Input.GetCursorPosition().ScreenPixels;
        int xAdjustment = xOffset.Value;
        int yAdjustment = yOffset.Value;

        // ModEntry.ModMonitor.Log($"{xAdjustment} - {yAdjustment}", LogLevel.Warn);

        int width = Game1.viewport.Width / 8;
        if (pos.X < width)
        {
            xAdjustment -= Config.Speed;
        }
        else if (pos.X > Game1.viewport.Width - width)
        {
            xAdjustment += Config.Speed;
        }

        int height = Game1.viewport.Height / 8;
        if (pos.Y < height)
        {
            yAdjustment -= Config.Speed;
        }
        else if (pos.Y > Game1.viewport.Height - height)
        {
            yAdjustment += Config.Speed;
        }

        xAdjustment = Math.Clamp(xAdjustment, -Config.XRangeInternal, Config.XRangeInternal);
        yAdjustment = Math.Clamp(yAdjustment, -Config.YRangeInternal, Config.YRangeInternal);

        xOffset.Value = xAdjustment;
        yOffset.Value = yAdjustment;

        // Game1.moveViewportTo(new Vector2(Game1.player.Position.X + xOffset.Value, Game1.player.Position.Y + yOffset.Value), Config.Speed);
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
}