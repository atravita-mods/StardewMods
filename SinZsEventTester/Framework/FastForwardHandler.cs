﻿using Microsoft.Xna.Framework;
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
    private readonly int ratio;

    /// <summary>
    /// Initializes a new instance of the <see cref="FastForwardHandler"/> class.
    /// </summary>
    /// <param name="monitor">SMAPI monitor instance.</param>
    /// <param name="loopEvents">The gameloop event handler.</param>
    /// <param name="reflector">The reflection helper.</param>
    /// <param name="ratio">The ratio to fast forward.</param>
    internal FastForwardHandler(IMonitor monitor, IGameLoopEvents loopEvents, IReflectionHelper reflector, int ratio)
    {
        this._monitor = monitor;
        this._loopEvents = loopEvents;
        this._reflector = reflector;
        this.ratio = ratio;

        loopEvents.UpdateTicking += this.OnUpdateTicked;
    }

    /// <summary>
    /// Gets a value indicating whether or not this instance is disposed.
    /// </summary>
    internal bool IsDisposed { get; private set; } = false;

    private void OnUpdateTicked(object? sender, UpdateTickingEventArgs e)
    {
        if (this._modCalledTick || Context.ScreenId != 0)
        {
            return;
        }
        this._modCalledTick = true;
        try
        {
            for (int i = 0; i < this.ratio; i++)
            {
                if (this.IsDisposed)
                {
                    return;
                }

                Vector2 cachedPosition = Game1.player.Position;
                GameLocation? cachedMap = i > 2 ? Game1.player.currentLocation : null;

                this._reflector.GetMethod(Game1.game1, "Update").Invoke([Game1.currentGameTime]);

                if (cachedMap is not null && cachedMap == Game1.player.currentLocation)
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
                this._monitor?.Log("Disposing fast forward");
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
