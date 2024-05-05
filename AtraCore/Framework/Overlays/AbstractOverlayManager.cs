using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

namespace AtraCore.Framework.Overlays;

/// <summary>
/// An abstract class that handles a draw layer over the world.
/// </summary>
internal abstract class AbstractOverlayManager : IDisposable
{
    protected IMonitor _monitor { get; private set; }

    private IGameLoopEvents _events;
    private IDisplayEvents _draw;

    private WeakReference<Farmer> player;

    private bool disposedValue = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractOverlayManager"/> class.
    /// </summary>
    /// <param name="events">Gameloop events.</param>
    /// <param name="draw">Draw events.</param>
    /// <param name="monitor">logger.</param>
    /// <param name="player">Affected player.</param>
    internal AbstractOverlayManager(IGameLoopEvents events, IDisplayEvents draw, IMonitor monitor, Farmer player)
    {
        this._events = events;
        events.UpdateTicked += this.OnTicked;
        this._draw = draw;
        draw.RenderedWorld += this.RenderedWorld;
        this._monitor = monitor;
        this.player = new(player);
    }

    private void RenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (this.player?.TryGetTarget(out Farmer? target) == true
            && ReferenceEquals(target, Game1.player))
        {
            this.Draw(e.SpriteBatch);
        }
    }

    private void OnTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (this.player?.TryGetTarget(out Farmer? target) == true
            && ReferenceEquals(target, Game1.player))
        {
            this.Tick();
        }
    }

    protected abstract void Tick();

    protected abstract void Draw(SpriteBatch b);

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            this._draw.RenderedWorld -= this.RenderedWorld;
            this._events.UpdateTicked -= this.OnTicked;

            this._draw = null!;
            this._events = null!;
            this._monitor = null!;
            this.player = null!;
            this.disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~AbstractOverlayManager()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: false);
    }

    public void Dispose()
    {
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
