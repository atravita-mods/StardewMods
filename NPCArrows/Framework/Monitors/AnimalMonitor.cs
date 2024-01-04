using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NPCArrows.Framework.Interfaces;

namespace NPCArrows.Framework.Monitors;

/// <summary>
/// Monitors a single animal and draws the icon for it.
/// </summary>
internal sealed class AnimalMonitor(FarmAnimal animal) : IViewableWorldIcon
{
    public bool IsOnScreen => Utility.isOnScreen(this.animal.Position, 256);

    protected FarmAnimal animal { get; private set; } = animal;

    public void Draw(SpriteBatch sb) => throw new NotImplementedException();
}
