namespace CritterRings.Framework.Managers;
internal class JumpManager : IDisposable
{
    private const float gravity = 0.5f;

    private bool disposedValue;
    private WeakReference<Farmer> farmerRef;

    internal JumpManager(Farmer farmer) => this.farmerRef = new(farmer);

    private int distance;

    private enum State
    {
        Charging,
        Jumping,
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            this.disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~JumpManager()
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
