// Ignore Spelling: Clickable

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Menus;

using AtraUtils = AtraShared.Utils.Utils;

namespace DresserMiniMenu.Framework.MiniFarmerMenuIcons;

/// <summary>
/// A slot that corresponds to an inventory slot.
/// </summary>
/// <typeparam name="TObject">The type of the backing field.</typeparam>
internal class InventorySlot<TObject> : IInventorySlot<TObject>
    where TObject : Item
{
    /// <summary>
    /// The size of an inventory icon.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Reviewed.")]
    internal const int INVENTORY_SLOT_SIZE = 64; // px

    /// <summary>
    /// Initializes a new instance of the <see cref="InventorySlot{TObject}"/> class.
    /// </summary>
    /// <param name="type">The type of the inventory item.</param>
    /// <param name="x">The x location.</param>
    /// <param name="y">The y location.</param>
    /// <param name="name">The name of the component.</param>
    /// <param name="getItem">A function that gets the relevant item.</param>
    /// <param name="setItem">A function that sets an item into this slot.</param>
    /// <param name="isActive">Whether or not this slot should be active.</param>
    internal InventorySlot(EquipmentType type, int x, int y, string name, Func<TObject?> getItem, Action<TObject?> setItem, bool isActive = true)
    {
        this.Type = type;
        this.Clickable = new ClickableComponent(new Rectangle(x, y, INVENTORY_SLOT_SIZE, INVENTORY_SLOT_SIZE), name);
        this.GetItem = getItem;
        this.SetItem = setItem;
        this.IsActive = isActive;
    }

    /// <inheritdoc />
    public string Name => this.Clickable.name;

    /// <inheritdoc />
    public ClickableComponent Clickable { get; init; }

    /// <summary>
    /// Gets the type of wearable this inventory slot refers to.
    /// </summary>
    protected EquipmentType Type { get; init; }

    /// <summary>
    /// Gets a function that gets the wearable instance.
    /// </summary>
    protected Func<TObject?> GetItem { get; init; }

    /// <summary>
    /// Gets a function that sets the wearable instance.
    /// </summary>
    protected Action<TObject?> SetItem { get; init; }

    /// <summary>
    /// Gets a value indicating whether this inventory slot should be interact-able.
    /// </summary>
    protected bool IsActive { get; init; }

    /// <inheritdoc />
    public void Draw(SpriteBatch b)
    {
        TObject? item = this.GetItem();
        b.Draw(
            texture: Game1.menuTexture,
            destinationRectangle: this.Clickable.bounds,
            sourceRectangle: Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, item is null ? (int)this.Type : 10),
            color: this.IsActive ? Color.White : Color.DarkGray);
        item?.drawInMenu(
            b,
            new Vector2(this.Clickable.bounds.X, this.Clickable.bounds.Y),
            this.Clickable.scale,
            1f,
            0.866f,
            StackDrawType.Hide,
            this.IsActive ? Color.White : Color.SlateGray,
            true);
        if (!this.IsActive)
        {
            b.Draw(
                texture: AtraUtils.Pixel,
                destinationRectangle: this.Clickable.bounds,
                color: Color.DarkGray * 0.5f);
        }
    }

    /// <inheritdoc />
    public bool IsInBounds(int x, int y) => this.Clickable.containsPoint(x, y);

    /// <summary>
    /// Updates the position of this inventory slot.
    /// </summary>
    /// <param name="x">New X position.</param>
    /// <param name="y">New Y position.</param>
    public void UpdatePosition(int? x, int? y)
    {
        if (x is not null)
        {
            this.Clickable.bounds.X = x.Value;
        }
        if (y is not null)
        {
            this.Clickable.bounds.Y = y.Value;
        }
    }

    /// <inheritdoc />
    public bool TryHover(int x, int y, out Item? newHoveredItem)
    {
        if (this.IsInBounds(x, y))
        {
            newHoveredItem = this.GetItem();
            if (this.IsActive)
            {
                this.Clickable.scale = Math.Min(this.Clickable.scale + 0.05f, 1.1f);
            }
            return true;
        }
        newHoveredItem = null;
        this.Clickable.scale = Math.Max(1, this.Clickable.scale - 0.025f);
        return false;
    }

    /// <inheritdoc />
    public virtual bool CanAcceptItem(Item? item) => this.IsActive && (item is null || item is TObject);

    /// <inheritdoc />
    public virtual bool AssignItem(Item? item, out Item? prev, bool playSound)
    {
        if (this.IsActive && this.CanAcceptItem(item))
        {
            prev = this.GetItem();
            prev?.onDetachedFromParent();
            item?.onDetachedFromParent();
            Game1.player.Equip(prev as TObject, item as TObject, this.SetItem);

            if (playSound)
            {
                this.PlayEquipDequipSounds(item);
            }
            return true;
        }
        prev = null;
        return false;
    }

    private void PlayEquipDequipSounds(Item? item)
    {
        if (item is TObject)
        {
            this.Type.PlayEquipSound();
        }
        else
        {
            try
            {
                Game1.playSound("dwop");
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError("playing dequip sound", ex);
            }
        }
    }
}