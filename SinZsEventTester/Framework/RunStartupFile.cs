

using StardewModdingAPI.Events;

public class RunStartupFile: IDisposable
{
    private IMonitor _monitor;
    private IGameLoopEvents _loopEvents;

    private StreamReader? _reader;

    private int ticks_to_pause = 0;

    internal bool IsDisposed { get; private set; }

    public RunStartupFile(IMonitor monitor, IGameLoopEvents events, FileInfo file)
    {
        this._monitor = monitor;
        this._loopEvents = events;

        try
        {
            this._reader = new StreamReader(file.FullName);
            events.UpdateTicked += this.OnTick;

            Game1.isRunningMacro = true;
        }
        catch (Exception ex)
        {
            this._monitor.Log($"Failed reading file {file}.", LogLevel.Error);
            this._monitor.Log(ex.ToString());
            this.Dispose();
        }
    }

    private void OnTick(object? sender, UpdateTickedEventArgs e)
    {
        if (this.ticks_to_pause > 0)
        {
            --this.ticks_to_pause;
            return;
        }

        try
        {
            if (this._reader is not { } reader)
            {
                this.Dispose();
                return;
            }

            string? line = reader.ReadLine();
            if (line is null)
            {
                this.Dispose();
                return;
            }

            Game1.chatBox.textBoxEnter($"/{line}");
            this.ticks_to_pause = 10;
        }
        catch (Exception ex)
        {
            this._monitor.Log($"Error while trying to run startup macros", LogLevel.Error);
            this._monitor.Log(ex.ToString());
            this.Dispose();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.IsDisposed)
        {
            this._loopEvents.UpdateTicked -= this.OnTick;
            this._loopEvents = null!;
            this._monitor = null!;
            this._reader?.Dispose();
            this._reader = null!;
            Game1.isRunningMacro = false;
            this.IsDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}