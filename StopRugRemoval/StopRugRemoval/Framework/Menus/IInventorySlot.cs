using Microsoft.Xna.Framework.Graphics;

namespace StopRugRemoval.Framework.Menus;

/// <summary>
/// An inventory slot.
/// </summary>
/// <typeparam name="TObject">The type the inventory slot is.</typeparam>
internal interface IInventorySlot<out TObject>
    where TObject : Item
{
    /// <summary>
    /// Draws the inventory slot.
    /// </summary>
    /// <param name="b">Relevant spritebatch.</param>
    void Draw(SpriteBatch b);

    /// <summary>
    /// Tries to hover over this inventory slot.
    /// </summary>
    /// <param name="x">The x position of the mouse.</param>
    /// <param name="y">The y position of the mouse.</param>
    /// <param name="newHoveredItem">The relevant item being hovered over.</param>
    /// <returns>True if hovered over, false otherwise.</returns>
    bool TryHover(int x, int y, out Item? newHoveredItem);
}
