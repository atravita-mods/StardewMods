using StardewModdingAPI.Events;

namespace SinZsEventTester.Framework;

/// <summary>
/// Handles fast forward mode.
/// </summary>
internal sealed class FastForwardHandler : IDisposable
{
    private IMonitor _monitor;
    private IGameLoopEvents _loopEvents;
    private IReflectionHelper _reflector;
    private bool _modCalledTick;

    /// <summary>
    /// Initializes a new instance of the <see cref="FastForwardHandler"/> class.
    /// </summary>
    /// <param name="monitor">SMAPI monitor instance.</param>
    /// <param name="loopEvents">The gameloop event handler.</param>
    /// <param name="reflector">The reflection helper.</param>
    internal FastForwardHandler(IMonitor monitor, IGameLoopEvents loopEvents, IReflectionHelper reflector)
    {
        this._monitor = monitor;
        this._loopEvents = loopEvents;
        this._reflector = reflector;

        loopEvents.UpdateTicking += this.OnUpdateTicked;
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not this instance is disposed.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    private void OnUpdateTicked(object? sender, UpdateTickingEventArgs e)
    {
        if (this._modCalledTick || Context.ScreenId != 0)
        {
            return;
        }
        this._modCalledTick = true;
        try
        {
            for (int i = 0; i < ModEntry.Config.FastForwardRatio; i++)
            {
                if (this.IsDisposed)
                {
                    return;
                }

                var cachedPosition = Game1.player.Position;
                var cachedMap = Game1.player.currentLocation;
                this._reflector.GetMethod(Game1.game1, "Update").Invoke([Game1.currentGameTime]);
                if (cachedMap == Game1.player.currentLocation)
                {
                    Game1.player.Position = cachedPosition;
                }
            }
        }
        finally
        {
            this._modCalledTick = false;
        }
    }

    private void Dispose(bool disposing)
    {
        if (!this.IsDisposed)
        {
            if (disposing)
            {
                this._loopEvents.UpdateTicking -= this.OnUpdateTicked;
            }

            this._monitor = null!;
            this._loopEvents = null!;
            this._reflector = null!;
            this.IsDisposed = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~FastForwardHandler()
    {
         // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
         this.Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
