using AtraBase.Toolkit.Extensions;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

namespace CritterRings.Framework.Managers;
internal class JumpManager : IDisposable
{
    private const float GRAVITY = 0.5f;
    private const int DEFAULT_TICKS = 45;

    private bool disposedValue;
    private WeakReference<Farmer> farmerRef;

    private State state = State.Charging;
    private int distance = 1;
    private int ticks = DEFAULT_TICKS;
    private Vector2 direction = Vector2.Zero;

    private IGameLoopEvents gameEvents;
    private IDisplayEvents displayEvents;

    /// <summary>
    /// Initializes a new instance of the <see cref="JumpManager"/> class.
    /// </summary>
    /// <param name="farmer">The farmer we're tracking.</param>
    /// <param name="gameEvents">The game event manager.</param>
    /// <param name="displayEvents">The display event manager.</param>
    internal JumpManager(Farmer farmer, IGameLoopEvents gameEvents, IDisplayEvents displayEvents)
    {
        this.farmerRef = new(farmer);
        this.gameEvents = gameEvents;
        this.displayEvents = displayEvents;

        this.gameEvents.UpdateTicked += this.OnUpdateTicked;
        this.displayEvents.RenderedWorld += this.OnRenderedWorld;
    }

    private enum State
    {
        Inactive,
        Charging,
        Jumping,
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
                if (!ModEntry.Config.FrogRingButton.IsDown())
                {
                    ModEntry.ModMonitor.DebugOnlyLog($"(Frog Ring) Switching Charging -> Jumping");
                    this.state = State.Jumping;
                    Game1.player.synchronizedJump(16f);
                    Game1.player.CanMove = false;
                }
                else
                {
                    if (--this.ticks <= 0)
                    {
                        this.distance++;
                        this.ticks = DEFAULT_TICKS;
                    }
                    this.direction = Game1.player.FacingDirection switch
                    {
                        Game1.up => -Vector2.UnitY,
                        Game1.left => -Vector2.UnitX,
                        Game1.down => Vector2.UnitY,
                        _ => Vector2.UnitX,
                    };
                }
                break;
            case State.Jumping:
                if (Game1.player.yJumpOffset == 0 && Game1.player.yJumpVelocity.WithinMargin(0f))
                {
                    ModEntry.ModMonitor.DebugOnlyLog($"(Frog Ring) Switching Jumping -> Inactive");
                    this.state = State.Inactive;
                    Game1.player.CanMove = true;
                    this.Dispose();
                }
                break;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            this.Unhook();
            if (this.farmerRef?.TryGetTarget(out Farmer? farmer) == true && farmer is not null)
            {
                farmer.CanMove = true;
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

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
    }
}
