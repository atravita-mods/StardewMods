#define TRACE

using AtraBase.Toolkit.Extensions;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

namespace CritterRings.Framework.Managers;

/// <summary>
/// Manages a jump for a player.
/// </summary>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Preference.")]
internal sealed class JumpManager : IDisposable
{
    private const float GRAVITY = 0.5f;
    private const int DEFAULT_TICKS = 200;

    // event handlers.
    private IGameLoopEvents gameEvents;
    private IDisplayEvents displayEvents;

    private bool disposedValue;
    private WeakReference<Farmer> farmerRef;
    private bool previousCollisionValue = false; // keeps track of whether or not the farmer had noclip on.

    private State state = State.Charging;
    private int ticks = DEFAULT_TICKS;
    private readonly Vector2 direction = Vector2.Zero;

    // charging fields.
    private int distance = 1;
    private Vector2 currentTile = Vector2.Zero;
    private Vector2 openTile = Vector2.Zero;
    private bool isCurrentTileBlocked = false;

    // jumping fields.
    private JumpFrame frame;

    /// <summary>
    /// Initializes a new instance of the <see cref="JumpManager"/> class.
    /// </summary>
    /// <param name="farmer">The farmer we're tracking.</param>
    /// <param name="gameEvents">The game event manager.</param>
    /// <param name="displayEvents">The display event manager.</param>
    internal JumpManager(Farmer farmer, IGameLoopEvents gameEvents, IDisplayEvents displayEvents)
    {
        ModEntry.ModMonitor.DebugOnlyLog("(FrogRing) Starting -> Charging");
        this.farmerRef = new(farmer);
        this.gameEvents = gameEvents;
        this.displayEvents = displayEvents;

        this.gameEvents.UpdateTicked += this.OnUpdateTicked;
        this.displayEvents.RenderedWorld += this.OnRenderedWorld;

        this.direction = Game1.player.FacingDirection switch
        {
            Game1.up => -Vector2.UnitY,
            Game1.left => -Vector2.UnitX,
            Game1.down => Vector2.UnitY,
            _ => Vector2.UnitX,
        };

        farmer.CanMove = false;
        farmer.UsingTool = false;
        ModEntry.ModMonitor.DebugOnlyLog($"Picking tool? {Game1.pickingTool}", LogLevel.Alert);
        SetCrouchAnimation(farmer);
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="JumpManager"/> class.
    /// </summary>
    ~JumpManager() => this.Dispose(false);

    private enum State
    {
        Inactive,
        Charging,
        Jumping,
    }

    private enum JumpFrame
    {
        Start,
        Transition,
        Hold,
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Checks to see if this JumpManager is valid (ie, not disposed, and has an active farmer associated).
    /// </summary>
    /// <returns>True if valid.</returns>
    internal bool IsValid()
        => !this.disposedValue && this.state != State.Inactive && this.farmerRef?.TryGetTarget(out Farmer? farmer) == true && farmer is not null;

    private bool IsCurrentFarmer()
        => this.farmerRef?.TryGetTarget(out Farmer? farmer) == true && ReferenceEquals(farmer, Game1.player);

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!this.IsCurrentFarmer())
        {
            return;
        }
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!this.IsCurrentFarmer())
        {
            return;
        }
        switch (this.state)
        {
            case State.Charging:
                if (ModEntry.Config.FrogRingButton.IsDown())
                {
                    this.ticks -= ModEntry.Config.JumpChargeSpeed;
                    if (this.ticks <= 0)
                    {
                        if (this.distance < ModEntry.Config.MaxFrogJumpDistance)
                        {
                            ++this.distance;
                            CRUtils.PlayChargeCue(this.distance);
                        }
                        this.ticks = DEFAULT_TICKS;
                        ModEntry.ModMonitor.TraceOnlyLog($"(Frog Ring) distance: {this.distance}");
                    }
                }
                else
                {
                    ModEntry.ModMonitor.DebugOnlyLog($"(Frog Ring) Switching Charging -> Jumping", LogLevel.Info);
                    this.state = State.Jumping;

                    float initialVelocity = 6f * MathF.Sqrt(this.distance);
                    Game1.player.synchronizedJump(initialVelocity);

                    this.previousCollisionValue = Game1.player.ignoreCollisions;
                    Game1.player.ignoreCollisions = true;

                    StartJumpAnimation(Game1.player);
                }
                break;
            case State.Jumping:
                if (Game1.player.yJumpOffset == 0 && Game1.player.yJumpVelocity.WithinMargin(0f))
                {
                    ModEntry.ModMonitor.DebugOnlyLog($"(Frog Ring) Switching Jumping -> Inactive", LogLevel.Info);
                    this.state = State.Inactive;
                    this.Dispose();
                }
                else
                {
                    if (this.frame != JumpFrame.Hold)
                    {
                        switch (this.frame)
                        {
                            case JumpFrame.Start:
                            {
                                if (Game1.player.yJumpOffset < -20)
                                {
                                    ModEntry.ModMonitor.TraceOnlyLog("(Frog Ring) Setting Jump Frame: START -> TRANSITION");
                                    SetTransitionAnimation(Game1.player);
                                    this.frame = JumpFrame.Transition;
                                }
                                break;
                            }
                            case JumpFrame.Transition:
                            {
                                if (Game1.player.yJumpVelocity < 0)
                                {
                                    ModEntry.ModMonitor.TraceOnlyLog("(Frog Ring) Setting Jump Frame: TRANSITION -> HOLD");
                                    HoldJumpAnimation(Game1.player);
                                    this.frame = JumpFrame.Hold;
                                }
                                break;
                            }
                        }
                    }
                }
                break;
        }
    }

    #region cleanup

    private void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            this.Unhook();
            if (this.farmerRef?.TryGetTarget(out Farmer? farmer) == true && farmer is not null)
            {
                farmer.CanMove = true;
                farmer.ignoreCollisions = this.previousCollisionValue;
                farmer.jitterStrength = 0f;
                farmer.completelyStopAnimatingOrDoingAction();
            }
            this.farmerRef = null!;
            this.gameEvents = null!;
            this.displayEvents = null!;
            this.disposedValue = true;
        }
    }

    private void Unhook()
    {
        if (this.gameEvents is not null)
        {
            this.gameEvents.UpdateTicked -= this.OnUpdateTicked;
        }
        if (this.displayEvents is not null)
        {
            this.displayEvents.RenderedWorld -= this.OnRenderedWorld;
        }
    }

    #endregion

    #region animationFrames

    /***************************************************************************
     * Animations here are mostly from the watering and hoe-ing animations
     * and are made basically by inspecting FarmerSprite.getAnimationFromIndex
     * and Tool.endUsing.
     ***************************************************************************/

    private static void SetCrouchAnimation(Farmer farmer)
    {
        farmer.completelyStopAnimatingOrDoingAction();
        farmer.FarmerSprite.setCurrentSingleFrame(
            which: farmer.FacingDirection switch
            {
                Game1.down => 54,
                Game1.right => 58,
                Game1.up => 62,
                _ => 58,
            }, flip: farmer.FacingDirection == Game1.left);
        farmer.FarmerSprite.PauseForSingleAnimation = true;
    }

    private static void StartJumpAnimation(Farmer farmer)
    {
        farmer.FarmerSprite.setCurrentSingleFrame(
            which: farmer.FacingDirection switch
            {
                Game1.down => 55,
                Game1.right => 59,
                Game1.up => 63,
                _ => 59,
            }, flip: farmer.FacingDirection == Game1.left);
        farmer.FarmerSprite.PauseForSingleAnimation = true;
    }

    private static void SetTransitionAnimation(Farmer farmer)
    {
        farmer.FarmerSprite.setCurrentSingleFrame(
            which: farmer.FacingDirection switch
            {
                Game1.down => 25,
                Game1.right => 45,
                Game1.up => 46,
                _ => 45,
            }, flip: farmer.FacingDirection == Game1.left,
            secondaryArm: true);
        farmer.FarmerSprite.PauseForSingleAnimation = true;
    }

    private static void HoldJumpAnimation(Farmer farmer)
    {
        farmer.FarmerSprite.setCurrentSingleFrame(
        which: farmer.FacingDirection switch
        {
            Game1.down => 62,
            Game1.right => 52,
            Game1.up => 70,
            _ => 52,
        },
        //secondaryArm: true,
        flip: farmer.FacingDirection == Game1.left);
        farmer.FarmerSprite.PauseForSingleAnimation = true;
    }

    #endregion
}
