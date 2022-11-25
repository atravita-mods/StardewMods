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
            if (disposing)
            {
                Game1.player.team.specialOrders.OnElementChanged -= this.SpecialOrders_OnElementChanged;
                Game1.player.team.specialOrders.OnArrayReplaced -= this.SpecialOrders_OnArrayReplaced;
            }

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
            return added;
        }
        else
        {
            return Enumerable.Empty<string>();
        }
    }

    private void SpecialOrders_OnArrayReplaced(Netcode.NetList<SpecialOrder, Netcode.NetRef<SpecialOrder>> list, IList<SpecialOrder> before, IList<SpecialOrder> after)
    {

    }

    private void SpecialOrders_OnElementChanged(Netcode.NetList<SpecialOrder, Netcode.NetRef<SpecialOrder>> list, int index, SpecialOrder oldValue, SpecialOrder newValue)
    {
        if (this.added.Contains(oldValue.questKey.Value))
        {

        }
    }
}