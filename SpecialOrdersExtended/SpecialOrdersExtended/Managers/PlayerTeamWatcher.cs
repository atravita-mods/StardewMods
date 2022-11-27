using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpecialOrdersExtended.Managers;
internal class PlayerTeamWatcher: IDisposable
{
    private bool isDisposed;
    private HashSet<string> added = new();
    private HashSet<string> removed = new();


    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerTeamWatcher"/> class.
    /// </summary>
    internal PlayerTeamWatcher()
    {
        Game1.player.team.completedSpecialOrders.OnValueAdded += this.OnValueAdded;
        Game1.player.team.completedSpecialOrders.OnValueRemoved += this.OnValueRemoved;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="PlayerTeamWatcher"/> class.
    /// </summary>
    ~PlayerTeamWatcher()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    protected virtual void Dispose(bool disposing)
    {
        if (!this.isDisposed)
        {
            Game1.player.team.completedSpecialOrders.OnValueAdded -= this.OnValueAdded;
            Game1.player.team.completedSpecialOrders.OnValueRemoved -= this.OnValueRemoved;

            this.added = null!;
            this.removed = null!;
            this.isDisposed = true;
        }
    }

    private void OnValueAdded(string key, bool value)
    {
        if (key is not null && !this.removed.Remove(key))
        {
            this.added.Add(key);
        }
    }

    private void OnValueRemoved(string key, bool value)
    {
        if (key is not null && !this.added.Remove(key))
        {
            this.removed.Add(key);
        }
    }

    internal void Reset()
    {
        this.removed.Clear();
        this.added.Clear();
    }

    internal IEnumerable<string> Check()
    {
        if (this.added.Count > 0)
        {
            HashSet<string>? added = this.added;
            this.added = new();
            this.Reset();
            return added;
        }
        else
        {
            this.Reset();
            return Enumerable.Empty<string>();
        }
    }
}