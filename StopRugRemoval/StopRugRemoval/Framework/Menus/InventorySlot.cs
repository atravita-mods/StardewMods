using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Menus;

namespace StopRugRemoval.Framework.Menus;

/// <summary>
/// A slot that corresponds to an inventory slot.
/// </summary>
/// <typeparam name="TObject">The type of the backing field.</typeparam>
internal sealed class InventorySlot<TObject> : IInventorySlot<TObject>
    where TObject : Item
{
    private readonly InventorySlotType type;
    private readonly ClickableComponent clickable;
    private readonly Func<TObject?> getItem;
    private readonly Action<TObject?> setItem;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventorySlot{TObject}"/> class.
    /// </summary>
    /// <param name="type">The type of the inventory item.</param>
    /// <param name="x">The x location.</param>
    /// <param name="y">The y location.</param>
    /// <param name="name">The name of the component.</param>
    /// <param name="getItem">A function that gets the relevant item.</param>
    /// <param name="setItem">A function that sets an item into this slot.</param>
    internal InventorySlot(InventorySlotType type, int x, int y, string name, Func<TObject?> getItem, Action<TObject?> setItem)
    {
        this.type = type;
        this.clickable = new ClickableComponent(new Rectangle(x, y, 64, 64), name);
        this.getItem = getItem;
        this.setItem = setItem;
    }

    /// <inheritdoc />
    public void Draw(SpriteBatch b)
    {
        TObject? item = this.getItem();
        b.Draw(
            texture: Game1.menuTexture,
            destinationRectangle: this.clickable.bounds,
            sourceRectangle: Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, item is null ? (int)this.type : 10),
            color: Color.White);
        item?.drawInMenu(b, new Vector2(this.clickable.bounds.X, this.clickable.bounds.Y), this.clickable.scale, 1f, 0.866f, StackDrawType.Hide);
    }

    /// <inheritdoc />
    public bool TryHover(int x, int y, out Item? newHoveredItem)
    {
        if (this.clickable.containsPoint(x, y))
        {
            newHoveredItem = this.getItem();
            this.clickable.scale = Math.Min(this.clickable.scale + 0.05f, 1.1f);
            return true;
        }
        newHoveredItem = null;
        this.clickable.scale = Math.Max(1, this.clickable.scale - 0.025f);
        return false;
    }

    internal bool AssignItem(Item item, out Item? prev)
    {
        if (item is null || item is TObject)
        {
            prev = this.getItem();
            this.setItem(item as TObject);
            return true;
        }
        prev = null;
        return false;
    }
}

/// <summary>
/// The type of item this is. Used for drawing the menu background.
/// </summary>
internal enum InventorySlotType
{
    /// <summary>
    /// A <see cref="StardewValley.Objects.Hat"/>
    /// </summary>
    Hat = 42,

    /// <summary>
    /// A <see cref="StardewValley.Objects.Ring"/>
    /// </summary>
    Ring = 41,

    /// <summary>
    /// A pair of <see cref="StardewValley.Objects.Boots"/>
    /// </summary>
    Boots = 40,

    /// <summary>
    /// A <see cref="StardewValley.Objects.Clothing"/>
    /// </summary>
    Clothing = 69,
}