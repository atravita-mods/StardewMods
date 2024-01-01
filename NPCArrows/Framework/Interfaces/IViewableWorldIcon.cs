using Microsoft.Xna.Framework.Graphics;

namespace NPCArrows.Framework.Interfaces;

/// <summary>
/// An icon drawn in the world.
/// </summary>
internal interface IViewableWorldIcon
{
    internal void Draw(SpriteBatch sb);

    public bool IsOnScreen { get; }
}
