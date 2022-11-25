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

    internal PlayerTeamWatcher()
    {
        Game1.player.team.specialOrders.OnElementChanged += this.SpecialOrders_OnElementChanged;
        Game1.player.team.specialOrders.OnArrayReplaced += this.SpecialOrders_OnArrayReplaced;
    }

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

            Game1.player.team.specialOrders.OnElementChanged -= this.SpecialOrders_OnElementChanged;
            Game1.player.team.specialOrders.OnArrayReplaced -= this.SpecialOrders_OnArrayReplaced;

            this.added = null!;
            this.removed = null!;
            this.isDisposed = true;
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

    private void SpecialOrders_OnArrayReplaced(Netcode.NetList<SpecialOrder, Netcode.NetRef<SpecialOrder>> list, IList<SpecialOrder> before, IList<SpecialOrder> after)
    {
        HashSet<string> added = after.Select((q) => q.questKey.Value).ToHashSet();
        added.ExceptWith(before.Select(q => q.questKey.Value));
        HashSet<string> removed = before.Select((q) => q.questKey.Value).ToHashSet();
        removed.ExceptWith(after.Select(q => q.questKey.Value));

        foreach (string? key in removed)
        {
            this.Remove(key);
        }

        foreach (string? key in added)
        {
            this.Add(key);
        }
    }

    private void SpecialOrders_OnElementChanged(Netcode.NetList<SpecialOrder, Netcode.NetRef<SpecialOrder>> list, int index, SpecialOrder oldValue, SpecialOrder newValue)
    {
        this.Remove(oldValue.questKey.Value);
        this.Add(newValue.questKey.Value);
    }

    private void Add(string key)
    {
        if (key is null)
        {
            return;
        }
        if (!this.removed.Remove(key))
        {
            this.added.Add(key);
        }
    }

    private void Remove(string key)
    {
        if (key is null)
        {
            return;
        }
        if (!this.added.Remove(key))
        {
            this.removed.Add(key);
        }
    }
}