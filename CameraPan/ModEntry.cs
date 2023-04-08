using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraShared.ConstantsAndEnums;
using AtraShared.Integrations;
using AtraShared.Utils.Extensions;

using CameraPan.Framework;
using CameraPan.HarmonyPatches;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

using AtraUtils = AtraShared.Utils.Utils;

namespace CameraPan;

// TODO: draw a big arrow pointing towards the player if the player is off screen?

/// <inheritdoc />
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Constants.")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Reviewed.")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Accessors kept near backing fields.")]
internal sealed class ModEntry : Mod
{
    /// <summary>
    /// The integer ID of the camera item.
    /// </summary>
    internal const int CAMERA_ID = 106;

    /// <summary>
    /// The integer ID used to note hud messages from this mod.
    /// </summary>
    internal const int HUD_ID = -9553485;

    private static readonly PerScreen<Point> offset = new(() => Point.Zero);
    private static readonly PerScreen<Point> target = new (() => Point.Zero);

    /// <summary>
    /// Gets a value indicating the target point of the camera.
    /// </summary>
    internal static Point Target => target.Value;

    private static readonly PerScreen<bool> enabled = new(() => Config?.ToggleBehavior != ToggleBehavior.Never);

    /// <summary>
    /// Gets or sets a value indicating whether or not panning has been enabled.
    /// </summary>
    internal static bool IsEnabled
    {
        get => enabled.Value;
        set => enabled.Value = value;
    } 

    private static readonly PerScreen<bool> snapOnNextTick = new(() => true);

    /// <summary>
    /// Gets or sets a value indicating whether not the camera should snap to the target the next tick.
    /// </summary>
    internal static bool SnapOnNextTick
    {
        get => snapOnNextTick.Value;
        set => snapOnNextTick.Value = value;
    }

    private static readonly PerScreen<int> msHoldOffset = new(() => -1);

    internal static int MSHoldOffset
    {
        get => msHoldOffset.Value;
        set => msHoldOffset.Value = value;
    }

    /// <summary>
    /// Gets the config instance for this mod.
    /// </summary>
    internal static ModConfig Config { get; private set; } = null!;

    /// <summary>
    /// Gets the logging instance for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; } = null!;

    private GMCMHelper? gmcm;

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

        ConsoleCommands.Register(helper.ConsoleCommands);
        AssetManager.Initialize(helper.GameContent);

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    /// <inheritdoc />
    public override object? GetApi(IModInfo mod) => new API(mod.Manifest.UniqueID);

    #region reset

    /* Both of these carry the AggressiveInlining attribute
     * primarily because they're sometimes used as delegates. */

    /// <summary>
    /// Resets the target point to the feet of the player.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Reset()
    {
        offset.Value = Point.Zero;
        target.Value = new(Game1.player.getStandingX(), Game1.player.getStandingY());
    }

    /// <summary>
    /// Sets the offset to zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ZeroOffset() => offset.Value = Point.Zero;

    #endregion

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
            string message = I18n.Enabled_Message(enabled.Value ? I18n.Enabled() : I18n.Disabled());
            this.Monitor.Log(message);
            Game1.hudMessages.RemoveAll(message => message.number == HUD_ID);
            Game1.addHUDMessage(new(message, HUDMessage.newQuest_type) { number = HUD_ID, noIcon = true});
        }
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
        Config.RecalculateBounds();

        // Register for events.
        this.Helper.Events.GameLoop.UpdateTicked += this.OnTicked;
        this.Helper.Events.Display.RenderedHud += this.DrawHud;
        this.Helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
        this.Helper.Events.Player.Warped += this.OnWarped;
        this.Helper.Events.Display.WindowResized += static (_, _) => Config?.RecalculateBounds();

        // asset events.
        this.Helper.Events.Content.AssetRequested += static (_, e) => AssetManager.Apply(e);
        this.Helper.Events.Content.AssetsInvalidated += static (_, e) => AssetManager.Reset(e.NamesWithoutLocale);

        this.SetUpInitialConfig();

        this.Helper.Events.GameLoop.ReturnedToTitle += (_, _) => this.SetUpInitialConfig();
    }

    private void SetUpInitialConfig()
    {
        if (this.gmcm?.HasGottenAPI == true)
        {
            this.gmcm.Unregister();
        }

        this.gmcm ??= new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (this.gmcm.TryGetAPI())
        {
            this.gmcm.Register(
                reset: static () => Config = new(),
                save: () =>
                {
                    this.Helper.AsyncWriteConfig(this.Monitor, Config);
                    Config.RecalculateBounds();
                    UpdateBehaviorForConfig();
                    ViewportAdjustmentPatches.SetCameraBehaviorForConfig(Config, Game1.currentLocation);
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
            ViewportAdjustmentPatches.SetCameraBehaviorForConfig(Config, e.NewLocation);
        }
    }

    #region draw

    [MethodImpl(TKConstants.Hot)]
    private void DrawHud(object? sender, RenderedHudEventArgs e)
    {
        if (ConsoleCommands.DrawMarker)
        {
            Vector2 target = Game1.GlobalToLocal(Game1.viewport, Target.ToVector2());
            e.SpriteBatch.Draw(
                texture: AssetManager.DartsTexture,
                position: target,
                sourceRectangle: new Rectangle(0, 320, 64, 64),
                color: (Game1.viewportTarget.X != -2.14748365E+09f ? Color.Red : Color.White) * 0.7f,
                rotation: 0f,
                origin: new Vector2(32f, 32f),
                scale: 0.5f,
                effects: SpriteEffects.None,
                layerDepth: 1f);
        }

        if (Game1.currentLocation is not GameLocation location || !Context.IsPlayerFree)
        {
            return;
        }

        if (Context.IsMultiplayer && Config.ShowArrowsToOtherPlayers)
        {
            foreach (Farmer? player in location.farmers)
            {
                DrawArrowForPlayer(e.SpriteBatch, player);
            }
        }
        else if (!Config.KeepPlayerOnScreen)
        {
            DrawArrowForPlayer(e.SpriteBatch, Game1.player);
        }
    }

    [MethodImpl(TKConstants.Hot)]
    private static void DrawArrowForPlayer(SpriteBatch s, Farmer farmer)
    {
        if (farmer is null)
        {
            return;
        }

        Vector2 pos = farmer.Position + new Vector2(32f, -64f);

        Vector2 arrowPos = Game1.GlobalToLocal(pos);
        Direction direction = Direction.None;

        if (arrowPos.X <= 0)
        {
            direction |= Direction.Left;
            arrowPos.X = 8f;
        }
        else if (arrowPos.X >= Game1.viewport.Width)
        {
            direction |= Direction.Right;
            arrowPos.X = Game1.viewport.Width - 8f;
        }

        if (arrowPos.Y <= 0)
        {
            direction |= Direction.Up;
            arrowPos.Y = 8f;
        }
        else if (arrowPos.Y >= Game1.viewport.Height)
        {
            direction |= Direction.Down;
            arrowPos.Y = Game1.viewport.Height - 8f;
        }

        if (direction == Direction.None)
        {
            return;
        }

        arrowPos = Utility.snapToInt(arrowPos);

        s.Draw(
            texture: AssetManager.ArrowTexture,
            position: arrowPos,
            sourceRectangle: null,
            color: ReferenceEquals(farmer, Game1.player) ? Config.SelfColor : Config.FriendColor,
            rotation: direction.GetRotationFacing(),
            origin: new Vector2(2f, 2f),
            scale: Game1.pixelZoom,
            effects: SpriteEffects.None,
            layerDepth: 1f);

        farmer.FarmerRenderer.drawMiniPortrat(
            b: s,
            position: arrowPos - (direction.GetVectorFacing() * 48f) - new Vector2(32f, 48f),
            layerDepth: 1f,
            scale: Game1.pixelZoom,
            facingDirection: Game1.down,
            who: farmer);
    }

    #endregion

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
            if (MSHoldOffset > 0)
            {
                MSHoldOffset -= Game1.currentGameTime.ElapsedGameTime.Milliseconds;
            }
            if (Game1.player.CanMove && MSHoldOffset <= 0)
            {
                Vector2 pos = this.Helper.Input.GetCursorPosition().ScreenPixels;
                int width = Game1.viewport.Width / 8;
                if (Config.LeftButton.IsDown() || (Config.UseMouseToPan && pos.X < width && pos.X >= 0))
                {
                    xAdjustment -= Config.Speed;
                }
                else if (Config.RightButton.IsDown() || (Config.UseMouseToPan && pos.X > Game1.viewport.Width - width && pos.X <= Game1.viewport.Width))
                {
                    xAdjustment += Config.Speed;
                }

                int height = Game1.viewport.Height / 8;
                if (Config.UpButton.IsDown() || (Config.UseMouseToPan && pos.Y < height && pos.Y >= 0))
                {
                    yAdjustment -= Config.Speed;
                }
                else if (Config.DownButton.IsDown() || (Config.UseMouseToPan && pos.Y > Game1.viewport.Height - height && pos.Y <= Game1.viewport.Height))
                {
                    yAdjustment += Config.Speed;
                }

                xAdjustment = Math.Clamp(xAdjustment, -Config.XRangeInternal, Config.XRangeInternal);
                yAdjustment = Math.Clamp(yAdjustment, -Config.YRangeInternal, Config.YRangeInternal);
            }
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
            x = Math.Clamp(x, Game1.viewportCenter.X - Config.Speed, Game1.viewportCenter.X + Config.Speed);
            y = Math.Clamp(y, Game1.viewportCenter.Y - Config.Speed, Game1.viewportCenter.Y + Config.Speed);
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