using Microsoft.Xna.Framework;

using StardewModdingAPI.Events;

using StardewValley.Logging;

namespace SinZsEventTester.Framework;
internal class HiberationManager : IDisposable
{
    private IGameLoopEvents _events;
    private IReflectedField<IGameLogger> _logger;
    private int count;

    public HiberationManager(IGameLoopEvents events, IReflectionHelper reflector, int count)
    {
        this._events = events;
        this._events.UpdateTicked += this.OnUpdateTicked;

        this._logger = reflector.GetField<IGameLogger>(typeof(Game1), "log");
        this.count = count;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Game1.newDay || Game1.gameMode != Game1.playingGameMode || this.IsDisposed)
        {
            return;
        }
        if (!e.IsMultipleOf(10))
        {
            return;
        }
        if (--this.count <= 0)
        {
            this.Dispose();
            return;
        }
        var logger = this._logger.GetValue();
        logger.Info($"Okay, calling day update: {this.count} times remaining.");
        DebugCommands.DefaultHandlers.Water([], logger);
        DebugCommands.DefaultHandlers.DayUpdate(["DayUpdate", "1"], logger);

        Game1.currentLocation?.debris.Add(new Debris($"{this.count}", 1, Game1.player.Position, Color.White, 1f, 0f));
    }

    /// <summary>
    /// Gets a value indicating whether or not this instance is disposed.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.IsDisposed)
        {
            this._events.UpdateTicked -= this.OnUpdateTicked;
            this._events = null!;

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            this.IsDisposed = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~HiberationManager()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
