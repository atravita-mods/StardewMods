using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NPCArrows.Framework.Interfaces;

namespace NPCArrows.Framework.Monitors;

/// <summary>
/// Monitors a single animal and draws the icon for it.
/// </summary>
internal sealed class AnimalMonitor : IViewableWorldIcon, IDisposable
{
    private bool disposedValue;

    public bool IsOnScreen => Utility.isOnScreen(this.animal.Position, 256);

    protected FarmAnimal animal { get; private set; }

    public AnimalMonitor(FarmAnimal animal)
    {
        this.animal = animal;
    }

    public void Draw(SpriteBatch sb) => throw new NotImplementedException();

    private void Dispose(bool disposing)
    {
        if (!this.disposedValue)
        {
            this.animal = null!;

            this.disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
