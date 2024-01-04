namespace NPCArrows.Framework.Monitors;

using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;
using Netcode;

/// <summary>
/// Monitors friendships.
/// </summary>
internal abstract class AbstractFriendshipMonitor : IDisposable
{
    protected Friendship friendship { get; private set; }
    protected NPC npc { get; private set; }

    private bool disposedValue;

    #region delegates
    private static readonly Lazy<Func<Friendship, NetInt>> _giftsTodayGetter = new(static () =>
        typeof(Friendship).GetCachedField("giftsToday", ReflectionCache.FlagTypes.InstanceFlags)
                          .GetInstanceFieldGetter<Friendship, NetInt>());

    private static readonly Lazy<Func<Friendship, NetBool>> _talkedToGetter = new(static () =>
        typeof(Friendship).GetCachedField("talkedToToday", ReflectionCache.FlagTypes.InstanceFlags)
                          .GetInstanceFieldGetter<Friendship, NetBool>());
    #endregion

    internal AbstractFriendshipMonitor(Friendship friendship, NPC npc)
    {
        this.friendship = friendship;
        this.npc = npc;

        NetInt giftsToday = _giftsTodayGetter.Value(friendship);
        giftsToday.fieldChangeEvent += this.GiftsGiven;

        NetBool talkedTo = _talkedToGetter.Value(friendship);
        talkedTo.fieldChangeEvent += this.TalkedTo;
    }

    private void TalkedTo(NetBool field, bool oldValue, bool newValue)
    {
        if (newValue && !oldValue)
        {
            this.OnTalkedTo();
        }
    }

    private void GiftsGiven(NetInt field, int oldValue, int newValue)
    {
        if (oldValue != newValue)
        {
            this.OnGiftGiven(newValue - oldValue);
        }
    }

    protected abstract void OnGiftGiven(int number);

    protected abstract void OnTalkedTo();

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            if (disposing)
            {
                NetInt giftsToday = _giftsTodayGetter.Value(this.friendship);
                giftsToday.fieldChangeEvent -= this.GiftsGiven;

                NetBool talkedTo = _talkedToGetter.Value(this.friendship);
                talkedTo.fieldChangeEvent -= this.TalkedTo;
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            this.disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~AbstractFriendshipMonitor()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
