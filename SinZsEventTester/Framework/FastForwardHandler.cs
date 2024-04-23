using StardewModdingAPI.Events;

namespace SinZsEventTester.Framework;
internal sealed class FastForwardHandler : IDisposable
{
    private IMonitor _monitor;
    private IGameLoopEvents _loopEvents;
    private IReflectionHelper _reflector;
    private ModConfig _modConfig;
    private bool disposedValue;
    private bool _modCalledTick;

    internal FastForwardHandler(IMonitor monitor, IGameLoopEvents loopEvents, IReflectionHelper reflector, ModConfig config)
    {
        this._monitor = monitor;
        this._loopEvents = loopEvents;
        this._reflector = reflector;
        this._modConfig = config;

        loopEvents.UpdateTicking += this.OnUpdateTicked;
    }

    internal bool IsDisposed => this.disposedValue;

    private void OnUpdateTicked(object? sender, UpdateTickingEventArgs e)
    {
        if (this._modCalledTick)
        {
            return;
        }
        this._modCalledTick = true;
        for (int i = 0; i < this._modConfig.FastForwardRatio; i++)
        {
            this._reflector.GetMethod(Game1.game1, "Update").Invoke([Game1.currentGameTime]);
        }
        this._modCalledTick = false;
    }

    private void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            if (disposing)
            {
                this._loopEvents.UpdateTicking -= this.OnUpdateTicked;
            }

            this._monitor = null!;
            this._loopEvents = null!;
            this._reflector = null!;
            this._modConfig = null!;
            this.disposedValue = true;
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
