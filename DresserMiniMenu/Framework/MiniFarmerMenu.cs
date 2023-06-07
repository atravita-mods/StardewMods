using AtraShared.Utils.Extensions;

using DresserMiniMenu.Framework.Menus.MiniFarmerMenu;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Menus;
using StardewValley.Objects;

namespace DresserMiniMenu.Framework;

/// <summary>
/// A little mini menu for farmer customization, mostly taken from <see cref="InventoryPage"/>.
/// </summary>
internal sealed class MiniFarmerMenu : IClickableMenu
{
    private const int LEFTARROW = 1000;
    private const int RIGHTARROW = 1010;
    private const int EQUIPMENT = 1030;

    private static bool blockRingSlots = false;

    private readonly int lastFacingDirection;
    private readonly List<IInventorySlot<Item>> equipmentIcons = new();
    private ClickableComponent portrait;
    private Rectangle backdrop;

    private ClickableTextureComponent leftArrow;
    private ClickableTextureComponent rightArrow;

    #region hover
    private Item? hoverItem;
    private string? hoverText;
    private string? hoverTitle;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="MiniFarmerMenu"/> class.
    /// </summary>
    /// <param name="shopMenu">ShopMenu to hang out with.</param>
    /// <param name="farmer">The farmer instance for this menu.</param>
    internal MiniFarmerMenu(ShopMenu shopMenu, Farmer farmer)
        : base(shopMenu.xPositionOnScreen - 128, shopMenu.yPositionOnScreen + 480 - 16, 384, 256 - 4)
    {
        this.ShopMenu = shopMenu;
        this.FarmerRef = farmer;
        this.lastFacingDirection = farmer.FacingDirection;
        this.FarmerRef.faceDirection(Game1.down);
        this.AssignClickableComponents();
    }

    /// <summary>
    /// Gets the farmer instance associated with this menu.
    /// </summary>
    internal Farmer FarmerRef { get; init; }

    /// <summary>
    /// Gets the shop (dresser) menu associated with this instance.
    /// </summary>
    internal ShopMenu ShopMenu { get; init; }

    /// <inheritdoc />
    public override void performHoverAction(int x, int y)
    {
        this.leftArrow.tryHover(x, y);
        this.rightArrow.tryHover(x, y);

        foreach (IInventorySlot<Item> equip in this.equipmentIcons)
        {
            if (equip.TryHover(x, y, out Item? newHoveredItem))
            {
                if (newHoveredItem is null)
                {
                    this.hoverItem = null;
                    this.hoverText = this.hoverTitle = string.Empty;
                }
                else if (!ReferenceEquals(this.hoverItem, newHoveredItem))
                {
                    this.hoverItem = newHoveredItem;
                    this.hoverText = newHoveredItem.getDescription();
                    this.hoverTitle = newHoveredItem.DisplayName;
                }
                return;
            }
        }

        this.hoverItem = null;
        this.hoverText = this.hoverTitle = string.Empty;
    }

    /// <inheritdoc />
    public override void draw(SpriteBatch b)
    {
        // draw backdrop
        drawTextureBox(
            b,
            texture: Game1.mouseCursors,
            sourceRect: new Rectangle(384, 373, 18, 18),
            x: this.xPositionOnScreen,
            y: this.yPositionOnScreen,
            width: this.width,
            height: this.height,
            color: Color.White,
            scale: Game1.pixelZoom);

        // draw equipment icons
        foreach (IInventorySlot<Item> equip in this.equipmentIcons)
        {
            equip.Draw(b);
        }

        // draw farmer.
        bool isDarkOut = Game1.timeOfDay >= 1900;
        b.Draw(
            isDarkOut ? Game1.nightbg : Game1.daybg,
            this.backdrop,
            Color.White);
        FarmerRenderer.isDrawingForUI = true;

        // Much thanks to PeacefulEnd, who showed me the way to draw a spinny farmer correctly.
        this.FarmerRef.FarmerRenderer.draw(
            b,
            this.FarmerRef.FarmerSprite.CurrentAnimationFrame,
            this.FarmerRef.FarmerSprite.CurrentFrame,
            this.FarmerRef.FarmerSprite.SourceRect,
            new Vector2(this.portrait.bounds.X, this.portrait.bounds.Y),
            Vector2.Zero,
            0.8f,
            Color.White,
            0f,
            1f,
            this.FarmerRef);
        if (isDarkOut)
        {
            this.FarmerRef.FarmerRenderer.draw(
            b,
            this.FarmerRef.FarmerSprite.CurrentAnimationFrame,
            this.FarmerRef.FarmerSprite.CurrentFrame,
            this.FarmerRef.FarmerSprite.SourceRect,
            new Vector2(this.portrait.bounds.X, this.portrait.bounds.Y),
            Vector2.Zero,
            0.8f,
            Color.DarkBlue * 0.3f,
            0f,
            1f,
            this.FarmerRef);
        }

        FarmerRenderer.isDrawingForUI = false;

        this.leftArrow.draw(b);
        this.rightArrow.draw(b);

        if (!string.IsNullOrEmpty(this.hoverText))
        {
            drawToolTip(b, this.hoverText, this.hoverTitle, this.hoverItem);
        }
    }

    /// <inheritdoc />
    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        this.xPositionOnScreen = this.ShopMenu.xPositionOnScreen - 128;
        this.yPositionOnScreen = this.ShopMenu.yPositionOnScreen + 480 - 16;
        this.AssignClickableComponents();
    }

    /// <inheritdoc />
    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (this.leftArrow.containsPoint(x, y))
        {
            int facing = (this.FarmerRef.FacingDirection + 1) % 4;
            this.FarmerRef.faceDirection(facing);
        }
        else if (this.rightArrow.containsPoint(x, y))
        {
            int facing = (this.FarmerRef.FacingDirection + 3) % 4;
            this.FarmerRef.faceDirection(facing);
        }
        else
        {
            foreach (IInventorySlot<Item> item in this.equipmentIcons)
            {
                if (item.IsInBounds(x, y))
                {
                    Item heldItem = Utility.PerformSpecialItemPlaceReplacement(this.ShopMenu.heldItem as Item);
                    if (item.AssignItem(heldItem, out Item? prev, playSound))
                    {
                        this.ShopMenu.heldItem = Utility.PerformSpecialItemGrabReplacement(prev);
                    }
                }
            }
        }

        base.receiveLeftClick(x, y, playSound);
    }

    /// <inheritdoc />
    public override void emergencyShutDown()
    {
        base.emergencyShutDown();

        // try to not lose someone's items.
        // add to inventory, and if that doesn't work, add to the dresser
        // and if THAT doesn't work, drop it at the player's feet.
        if (this.ShopMenu.heldItem is not null && !this.FarmerRef.addItemToInventoryBool(this.ShopMenu.heldItem as Item)
            && !this.ShopMenu.onSell(this.ShopMenu.heldItem))
        {
            Game1.currentLocation.debris.Add(new(this.ShopMenu.heldItem as Item, this.FarmerRef.Position));
        }

        this.ShopMenu.heldItem = null;
    }

    /// <summary>
    /// Disables the ring slots.
    /// </summary>
    internal static void DisableRingSlots() => blockRingSlots = true;

    /// <summary>
    /// Checks to see if the menu can hold this item.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <returns>True if the menu can take this item.</returns>
    internal bool CanAcceptThisItem(Item item)
    {
        foreach (IInventorySlot<Item> slot in this.equipmentIcons)
        {
            if (slot.CanAcceptItem(item))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// A method to call before exiting the menu.
    /// </summary>
    internal void BeforeExit()
    {
        this.FarmerRef.faceDirection(this.lastFacingDirection);
    }

    /// <summary>
    /// Adds my clickable components to the menu's list.
    /// </summary>
    /// <param name="menu">Shopmenu to add to.</param>
    internal void AddClickables(ShopMenu menu)
    {
        menu.allClickableComponents.Add(this.leftArrow);
        menu.allClickableComponents.Add(this.rightArrow);

        foreach (IInventorySlot<Item> slot in this.equipmentIcons)
        {
            menu.allClickableComponents.Add(slot.Clickable);
        }
    }

    [MemberNotNull(nameof(portrait))]
    [MemberNotNull(nameof(leftArrow))]
    [MemberNotNull(nameof(rightArrow))]
    private void AssignClickableComponents()
    {
        this.backdrop = new Rectangle(
            x: this.xPositionOnScreen + (this.width / 2) - 64,
            y: this.yPositionOnScreen + 32,
            width: 128,
            height: 192);
        this.portrait = new ClickableComponent(
            new Rectangle(
            x: this.xPositionOnScreen + (this.width / 2) - 32,
            y: this.yPositionOnScreen + 64,
            width: 64,
            height: 96),
            name: "Portrait");

        this.leftArrow = new(new Rectangle(
            x: this.backdrop.X,
            y: this.backdrop.Bottom - 44,
            width: 48,
            height: 44),
            Game1.mouseCursors,
            new Rectangle(352, 495, 12, 11),
            Game1.pixelZoom)
        {
            myID = LEFTARROW,
            leftNeighborID = EQUIPMENT,
            rightNeighborID = RIGHTARROW,
        };
        this.rightArrow = new(new Rectangle(
            x: this.backdrop.Right - 48,
            y: this.backdrop.Bottom - 44,
            width: 48,
            height: 44),
            Game1.mouseCursors,
            new Rectangle(365, 495, 12, 11),
            Game1.pixelZoom)
        {
            myID = RIGHTARROW,
            leftNeighborID = LEFTARROW,
            rightNeighborID = EQUIPMENT + 4,
        };

        // equipment icons.
        this.equipmentIcons.Clear();
        this.equipmentIcons.Add(new RingSlot(
            x: this.xPositionOnScreen + 32,
            y: this.yPositionOnScreen + 32,
            name: "Left Ring",
            getItem: static () => Game1.player.leftRing.Value,
            setItem: static (value) => Game1.player.leftRing.Value = value,
            isActive: !blockRingSlots));
        this.equipmentIcons.Add(new RingSlot(
            x: this.xPositionOnScreen + 32,
            y: this.yPositionOnScreen + 32 + 64,
            name: "Right Ring",
            getItem: static () => Game1.player.rightRing.Value,
            setItem: static (value) => Game1.player.rightRing.Value = value,
            isActive: !blockRingSlots));
        this.equipmentIcons.Add(new BootsSlot(
            x: this.xPositionOnScreen + 32,
            y: this.yPositionOnScreen + 32 + 128,
            name: "Boots",
            getItem: static () => Game1.player.boots.Value,
            setItem: static value => Game1.player.boots.Value = value));
        this.equipmentIcons.Add(new InventorySlot<Hat>(
            type: InventorySlotType.Hat,
            x: this.xPositionOnScreen + this.width - 64 - 32,
            y: this.yPositionOnScreen + 32,
            name: "Hat",
            getItem: static () => Game1.player.hat.Value,
            setItem: static value => Game1.player.hat.Value = value));
        this.equipmentIcons.Add(new ClothingSlot(
            type: InventorySlotType.Shirt,
            x: this.xPositionOnScreen + this.width - 64 - 32,
            y: this.yPositionOnScreen + 32 + 64,
            name: "Shirt",
            getItem: static () => Game1.player.shirtItem.Value,
            setItem: static value => Game1.player.shirtItem.Value = value));
        this.equipmentIcons.Add(new ClothingSlot(
            type: InventorySlotType.Pants,
            x: this.xPositionOnScreen + this.width - 64 - 32,
            y: this.yPositionOnScreen + 32 + 128,
            name: "Pants",
            getItem: static () => Game1.player.pantsItem.Value,
            setItem: static value => Game1.player.pantsItem.Value = value));

        this.AssignIds();
    }

    private void AssignIds()
    {
        try
        {
            for (int i = 0; i < this.equipmentIcons.Count; i++)
            {
                IInventorySlot<Item> slot = this.equipmentIcons[i];
                slot.Clickable.myID = EQUIPMENT + i;

                // right edge
                if (i < 3)
                {
                    slot.Clickable.rightNeighborID = LEFTARROW;
                }
                else
                {
                    // the second row is to the right of the portrait.
                    if (i < 6)
                    {
                        slot.Clickable.leftNeighborID = RIGHTARROW;
                    }
                    else
                    {
                        slot.Clickable.leftNeighborID = EQUIPMENT + i - 3;
                    }

                    // assign the one to the right.
                    int right = i + 3;
                    if (right < this.equipmentIcons.Count)
                    {
                        slot.Clickable.rightNeighborID = EQUIPMENT + right;
                    }
                }

                // assign up and down.
                switch (i % 3)
                {
                    case 0: // top
                        slot.Clickable.downNeighborID = slot.Clickable.myID + 1;
                        slot.Clickable.upNeighborID = this.ShopMenu.forSaleButtons[^1].myID;
                        break;
                    case 1: // middle
                        slot.Clickable.downNeighborID = slot.Clickable.myID + 1;
                        slot.Clickable.upNeighborID = slot.Clickable.myID - 1;
                        break;
                    case 2: // bottom.
                        slot.Clickable.upNeighborID = slot.Clickable.myID - 1;
                        break;
                }
            }

            List<ClickableComponent> inventoryButtons = this.ShopMenu.inventory.GetBorder(InventoryMenu.BorderSide.Left);
            for (int i = this.equipmentIcons.Count - 1; i > this.equipmentIcons.Count - 4; i--)
            {
                var inventoryID = i % 3;
                if (inventoryButtons.Count <= inventoryID)
                {
                    continue;
                }
                IInventorySlot<Item> slot = this.equipmentIcons[i];
                ClickableComponent inventoryButton = inventoryButtons[inventoryID];

                slot.Clickable.rightNeighborID = inventoryButton.myID;
                inventoryButton.leftNeighborID = slot.Clickable.myID;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("assigning clickable component IDs", ex);
        }
    }
}
