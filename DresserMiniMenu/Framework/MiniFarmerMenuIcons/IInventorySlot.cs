using Microsoft.Xna.Framework.Graphics;

using StardewValley.Menus;

namespace DresserMiniMenu.Framework.MiniFarmerMenuIcons;

/// <summary>
/// An inventory slot.
/// </summary>
/// <typeparam name="TObject">The type the inventory slot is.</typeparam>
internal interface IInventorySlot<out TObject>
    where TObject : Item
{
    /// <summary>
    /// The name of this inventory slot.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the clickable component for this inventory slot.
    /// </summary>
    ClickableComponent Clickable { get; }

    /// <summary>
    /// Draws the inventory slot.
    /// </summary>
    /// <param name="b">Relevant spritebatch.</param>
    void Draw(SpriteBatch b);

    /// <summary>
    /// Checks to see if a point is within this inventory slot.
    /// </summary>
    /// <param name="x">pixel X.</param>
    /// <param name="y">pixel Y.</param>
    /// <returns>True if inside, false otherwise.</returns>
    bool IsInBounds(int x, int y);

    /// <summary>
    /// Tries to hover over this inventory slot.
    /// </summary>
    /// <param name="x">The x position of the mouse.</param>
    /// <param name="y">The y position of the mouse.</param>
    /// <param name="newHoveredItem">The relevant item being hovered over.</param>
    /// <returns>True if hovered over, false otherwise.</returns>
    bool TryHover(int x, int y, out Item? newHoveredItem);

    /// <summary>
    /// Gets whether or not this particular inventory slot can accept this item.
    /// </summary>
    /// <param name="item">Item to check.</param>
    /// <returns>True if acceptable, false otherwise.</returns>
    bool CanAcceptItem(Item? item);

    /// <summary>
    /// Tries to assign an item to this slot.
    /// </summary>
    /// <param name="item">The item to assign.</param>
    /// <param name="prev">The previous item assigned to this slot.</param>
    /// <param name="playSound">Whether or not to play a sound associated with this.</param>
    /// <returns>True if assigned, false otherwise.</returns>
    bool AssignItem(Item? item, out Item? prev, bool playSound);
}
