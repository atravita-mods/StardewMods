using System.Runtime.CompilerServices;

using AtraBase.Toolkit;
using AtraBase.Toolkit.Extensions;

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

// TODO: draw a big arrow pointing towards the player if the player is off screen?

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    /// <summary>
    /// The integer ID of the camera item.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
    internal const int CAMERA_ID = 106;

    private static readonly PerScreen<Point> offset = new(() => Point.Zero);
    private static readonly PerScreen<Point> target = new (() => Point.Zero);

    internal static Point Target => target.Value;

    private static readonly PerScreen<bool> enabled = new(() => !(Config?.ToggleBehavior != ToggleBehavior.Never));

    private static readonly PerScreen<bool> snapOnNextTick = new(() => false);

    /// <summary>
    /// Gets or sets a value indicating whether not the camera should snap to the target the next tick.
    /// </summary>
    internal static bool SnapOnNextTick
    {
        get => snapOnNextTick.Value;
        set => snapOnNextTick.Value = value;
    }

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

        if (Config.ToggleBehavior == ToggleBehavior.Never)
        {
            enabled.Value = false;
        }

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

        helper.Events.GameLoop.UpdateTicked += this.OnTicked;
        helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;

        helper.Events.Player.Warped += this.OnWarped;

        helper.Events.Display.WindowResized += static (_, _) => Config?.RecalculateBounds();
    }

    private static void Reset()
    {
        offset.Value = Point.Zero;
        target.Value = new(Game1.player.getStandingX(), Game1.player.getStandingY());
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (Config.ResetButton.JustPressed())
        {
            Reset();
            SnapOnNextTick = true;
        }

        if (Config.ToggleBehavior == ToggleBehavior.Toggle && Config.ToggleButton.JustPressed())
        {
            enabled.Value = !enabled.Value;
            this.Monitor.Log($"Switching panning to {enabled.Value}");
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

                    UpdateBehaviorForConfig();
                })
            .AddParagraph(I18n.Mod_Description)
            .GenerateDefaultGMCM(static () => Config);
        }
    }

    private static void UpdateBehaviorForConfig()
    {
        switch (Config.ToggleBehavior)
        {
            case ToggleBehavior.Never:
                foreach ((int screen, bool _) in enabled.GetActiveValues())
                {
                    enabled.SetValueForScreen(screen, false);
                }
                break;
            case ToggleBehavior.Camera:
            case ToggleBehavior.Toggle:
            case ToggleBehavior.Always:
                foreach ((int screen, bool _) in enabled.GetActiveValues())
                {
                    enabled.SetValueForScreen(screen, true);
                }
                break;
        }
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (e.IsLocalPlayer)
        {
            Reset();
            SnapOnNextTick = true;
        }
    }

    [MethodImpl(TKConstants.Hot)]
    private void OnTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsPlayerFree || !Game1.game1.IsActive)
        {
            return;
        }

        if (Config.ToggleBehavior == ToggleBehavior.Camera)
        {
                enabled.Value = Game1.player.ActiveObject is SObject obj && obj.bigCraftable.Value && obj.ParentSheetIndex == CAMERA_ID;
        }

        int xAdjustment = offset.Value.X;
        int yAdjustment = offset.Value.Y;
        if (enabled.Value)
        {
            Vector2 pos = this.Helper.Input.GetCursorPosition().ScreenPixels;
            int width = Game1.viewport.Width / 8;
            if (Config.LeftButton.IsDown() || (pos.X < width && pos.X >= 0))
            {
                xAdjustment -= Config.Speed;
            }
            else if (Config.RightButton.IsDown() || (pos.X > Game1.viewport.Width - width && pos.X <= Game1.viewport.Width))
            {
                xAdjustment += Config.Speed;
            }

            int height = Game1.viewport.Height / 8;
            if (Config.UpButton.IsDown() || (pos.Y < height && pos.Y >= 0))
            {
                yAdjustment -= Config.Speed;
            }
            else if (Config.DownButton.IsDown() || (pos.Y > Game1.viewport.Height - height && pos.Y <= Game1.viewport.Height))
            {
                yAdjustment += Config.Speed;
            }

            xAdjustment = Math.Clamp(xAdjustment, -Config.XRangeInternal, Config.XRangeInternal);
            yAdjustment = Math.Clamp(yAdjustment, -Config.YRangeInternal, Config.YRangeInternal);
        }
        else if (offset.Value != Point.Zero)
        {
            if (Math.Abs(xAdjustment) <= Config.Speed)
            {
                xAdjustment = 0;
            }
            else if (xAdjustment > 0)
            {
                xAdjustment -= Config.Speed;
            }
            else
            {
                xAdjustment += Config.Speed;
            }

            if (Math.Abs(yAdjustment) <= Config.Speed)
            {
                yAdjustment = 0;
            }
            else if (yAdjustment > 0)
            {
                yAdjustment -= Config.Speed;
            }
            else
            {
                yAdjustment += Config.Speed;
            }
        }

        offset.Value = new(xAdjustment, yAdjustment);

        int x = Game1.player.getStandingX() + xAdjustment;
        int y = Game1.player.getStandingY() + yAdjustment;

        if (SnapOnNextTick)
        {
            SnapOnNextTick = false;
        }
        else
        {
            if (Math.Abs(Game1.viewportCenter.X - x) < 512)
            {
                x = Math.Clamp(x, Game1.viewportCenter.X - Config.Speed, Game1.viewportCenter.X + Config.Speed);
            }
            else
            {
                ModMonitor.DebugOnlyLog($"snapped x", LogLevel.Info);
            }
            if (Math.Abs(Game1.viewportCenter.Y - y) < 512)
            {
                y = Math.Clamp(y, Game1.viewportCenter.Y - Config.Speed, Game1.viewportCenter.Y + Config.Speed);
            }
            else
            {
                ModMonitor.DebugOnlyLog($"snapped y", LogLevel.Info);
            }
        }

        // smooth it out a bit - if we're not moving very far just leave the camera in place.
        if (Math.Abs(Game1.viewportCenter.X - x) < 5)
        {
            x = Game1.viewportCenter.X;
        }
        if (Math.Abs(Game1.viewportCenter.Y - y) < 5)
        {
            y = Game1.viewportCenter.Y;
        }
        target.Value = new(x, y);
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