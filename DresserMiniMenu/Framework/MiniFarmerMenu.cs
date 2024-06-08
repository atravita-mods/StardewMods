using AtraShared.ConstantsAndEnums;
using AtraShared.Niceties;
using AtraShared.Utils.Extensions;

using DresserMiniMenu.Framework.MiniFarmerMenuIcons;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Menus;
using StardewValley.Objects;

using AtraUtils = AtraShared.Utils.Utils;

namespace DresserMiniMenu.Framework;

/// <summary>
/// A little mini menu for farmer customization, mostly taken from <see cref="InventoryPage"/>.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1214:Readonly fields should appear before non-readonly fields", Justification = "Fields ordered by use.")]
internal sealed class MiniFarmerMenu : IClickableMenu
{
    private const int LEFTARROW = 1000;
    private const int RIGHTARROW = 1010;
    private const int EQUIPMENT = 1030;

    private const int BASEWIDTH = 384;
    private const int BASEHEIGHT = 252;
    private static bool blockRingSlots = false;
    private readonly int lastFacingDirection;

    #region clickables
    private readonly List<IInventorySlot<Item>> equipmentIcons = new();
    private Rectangle portrait;
    private Rectangle backdrop;
    private ClickableTextureComponent rotateLeftArrow;
    private ClickableTextureComponent rotateRightArrow;

    // hair
    private ClickableTextureComponent? leftHairArrow;
    private ClickableTextureComponent? rightHairArrow;
    private int hairIndex;
    #endregion

    #region floating
    private Rectangle floating;
    private string? lastFilter;
    private readonly TextBox textbox = new(null, null, Game1.smallFont, Game1.textColor);
    private Rectangle effectiveTextboxArea;
    private Rectangle alphabetization;
    private AlphabetizationStatus status = AlphabetizationStatus.None;

    private readonly BaseColorFilter[] colorFilters;
    private BaseColorFilter? selectedFilter = null;
    #endregion

    #region hover
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1306:Field names should begin with lower-case letter", Justification = "Named for Lookup Anything.")]
    private Item? HoveredItem;
    private string? hoverText;
    private string? hoverTitle;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="MiniFarmerMenu"/> class.
    /// </summary>
    /// <param name="shopMenu">ShopMenu to hang out with.</param>
    /// <param name="farmer">The farmer instance for this menu.</param>
    internal MiniFarmerMenu(ShopMenu shopMenu, Farmer farmer)
        : base(shopMenu.xPositionOnScreen - 128, shopMenu.yPositionOnScreen + 480 - 16, BASEWIDTH, BASEHEIGHT)
    {
        this.ShopMenu = shopMenu;
        this.FarmerRef = farmer;
        this.lastFacingDirection = farmer.FacingDirection;
        this.FarmerRef.faceDirection(Game1.down);

        // color filters
        this.colorFilters =
        [
            new RegularColorFilter(Color.Black),
            new RegularColorFilter(Color.Gray),
            new RegularColorFilter(Color.White),
            new RegularColorFilter(Color.Red),
            new RegularColorFilter(Color.Yellow),
            new RegularColorFilter(Color.Green),
            new RegularColorFilter(Color.Blue),
            new RegularColorFilter(Color.Purple),
            new UnDyedColorFilter(),
            new PrismaticColorFilter(),
        ];

        this.AssignClickableComponents();

        // textbox functions
        this.textbox.OnBackspacePressed += this.OnBackspacePressed;
        this.textbox.OnTabPressed += this.OnEntered;
        this.textbox.OnEnterPressed += this.OnEntered;

        // hair
        this.hairIndex = Farmer.GetAllHairstyleIndices().IndexOf(farmer.hair.Value);
    }

    /// <summary>
    /// Gets the farmer instance associated with this menu.
    /// </summary>
    internal Farmer FarmerRef { get; init; }

    /// <summary>
    /// Gets the shop (dresser) menu associated with this instance.
    /// </summary>
    internal ShopMenu ShopMenu { get; init; }

    /// <summary>
    /// Gets a value indicating whether or not this menu should take all keyboard input.
    /// </summary>
    internal bool HasKeyboard => this.textbox.Selected;

    /// <inheritdoc />
    public override void performHoverAction(int x, int y)
    {
        this.leftHairArrow?.tryHover(x, y);
        this.rightHairArrow?.tryHover(x, y);

        this.rotateLeftArrow.tryHover(x, y);
        this.rotateRightArrow.tryHover(x, y);

        foreach (IInventorySlot<Item> equip in this.equipmentIcons)
        {
            if (equip.TryHover(x, y, out Item? newHoveredItem))
            {
                if (newHoveredItem is null)
                {
                    this.HoveredItem = null;
                    this.hoverText = this.hoverTitle = string.Empty;
                }
                else if (!ReferenceEquals(this.HoveredItem, newHoveredItem))
                {
                    this.HoveredItem = newHoveredItem;
                    this.hoverText = newHoveredItem.getDescription();
                    this.hoverTitle = newHoveredItem.DisplayName;
                }
                return;
            }
        }

        this.HoveredItem = null;
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

        // Much thanks to PeacefulEnd, who showed me the way to draw a spinny farmer correctly.
        FarmerRenderer.isDrawingForUI = true;
        this.FarmerRef.FarmerRenderer.draw(
            b,
            this.FarmerRef.FarmerSprite.CurrentAnimationFrame,
            this.FarmerRef.FarmerSprite.CurrentFrame,
            this.FarmerRef.FarmerSprite.SourceRect,
            new Vector2(this.portrait.X, this.portrait.Y),
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
            new Vector2(this.portrait.X, this.portrait.Y),
            Vector2.Zero,
            0.8f,
            Color.DarkBlue * 0.3f,
            0f,
            1f,
            this.FarmerRef);
        }
        FarmerRenderer.isDrawingForUI = false;

        if (ModEntry.Config.HairArrows)
        {
            string hair = this.FarmerRef.hair.Value.ToString();
            Vector2 hairSize = Game1.smallFont.MeasureString(hair);
            Utility.drawTextWithShadow(
                b,
                hair,
                Game1.smallFont,
                new(this.backdrop.X + (this.backdrop.Width / 2) - (hairSize.X / 2), this.backdrop.Y - 24),
                Game1.textColor);
            this.leftHairArrow?.draw(b);
            this.rightHairArrow?.draw(b);
        }

        this.rotateLeftArrow.draw(b);
        this.rotateRightArrow.draw(b);

        // floating bar
        this.textbox.Draw(b);
        drawTextureBox(
            b,
            texture: Game1.mouseCursors,
            sourceRect: new Rectangle(384, 373, 18, 18),
            x: this.alphabetization.X - 16,
            y: this.alphabetization.Y - 20,
            width: this.alphabetization.Width + 32,
            height: this.alphabetization.Height + 36,
            color: Color.White,
            scale: Game1.pixelZoom);
        b.Draw(
            AssetManager.Icons,
            this.alphabetization,
            new Rectangle(0, this.status == AlphabetizationStatus.Backward ? 48 : 0, 100, 48),
            this.status == AlphabetizationStatus.None ? Color.Gray * 0.7f : Color.White);
        foreach (BaseColorFilter color in this.colorFilters)
        {
            color.Draw(b, ReferenceEquals(this.selectedFilter, color));
        }

        if (!string.IsNullOrEmpty(this.hoverText))
        {
            drawToolTip(b, this.hoverText, this.hoverTitle, this.HoveredItem);
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
        if (this.rotateLeftArrow.containsPoint(x, y))
        {
            int facing = (this.FarmerRef.FacingDirection + 1) % 4;
            this.FarmerRef.faceDirection(facing);
        }
        else if (this.rotateRightArrow.containsPoint(x, y))
        {
            int facing = (this.FarmerRef.FacingDirection + 3) % 4;
            this.FarmerRef.faceDirection(facing);
        }
        else if (this.leftHairArrow?.containsPoint(x, y) == true)
        {
            List<int> all_hair = Farmer.GetAllHairstyleIndices();
            this.hairIndex = (this.hairIndex - 1 + all_hair.Count) % all_hair.Count;
            this.FarmerRef.changeHairStyle(all_hair[this.hairIndex]);
        }
        else if (this.rightHairArrow?.containsPoint(x, y) == true)
        {
            List<int> all_hair = Farmer.GetAllHairstyleIndices();
            this.hairIndex = (this.hairIndex + 1) % all_hair.Count;
            this.FarmerRef.changeHairStyle(all_hair[this.hairIndex]);
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
    public override void update(GameTime time)
    {
        base.update(time);
        this.textbox.Update();
    }

    /// <summary>
    /// Handles clicks on the floating submenu.
    /// </summary>
    /// <param name="x">X location.</param>
    /// <param name="y">Y location.</param>
    /// <param name="playSound">Whether or not sounds should be played.</param>
    /// <returns>Whether the click was handled.</returns>
    public bool TryClickFloatingElements(int x, int y, bool playSound = true)
    {
        if (this.floating.Contains(x, y))
        {
            if (this.effectiveTextboxArea.Contains(x, y))
            {
                this.textbox.SelectMe();
                if (playSound)
                {
                    PlayShwip();
                }
                return true;
            }
            if (this.alphabetization.Contains(x, y))
            {
                this.status++;
                if (this.status > AlphabetizationStatus.Backward)
                {
                    this.status = AlphabetizationStatus.None;
                }
                PlayShwip();
                this.PerformSort();
                return true;
            }
            foreach (BaseColorFilter filter in this.colorFilters)
            {
                if (filter.Contains(x, y))
                {
                    if (ReferenceEquals(filter, this.selectedFilter))
                    {
                        this.selectedFilter = null;
                    }
                    else
                    {
                        this.selectedFilter = filter;
                    }
                    PlayShwip();
                    this.ShopMenu.applyTab();
                    this.ApplyFilter();
                    return true;
                }
            }
        }
        return false;
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
        this.textbox.Selected = false;
    }

    /// <summary>
    /// Adds my clickable components to the menu's list.
    /// </summary>
    /// <param name="menu">Shopmenu to add to.</param>
    internal void AddClickables(ShopMenu menu)
    {
        menu.allClickableComponents.Add(this.rotateLeftArrow);
        menu.allClickableComponents.Add(this.rotateRightArrow);

        foreach (IInventorySlot<Item> slot in this.equipmentIcons)
        {
            menu.allClickableComponents.Add(slot.Clickable);
        }

        if (this.rightHairArrow is not null)
        {
            menu.allClickableComponents.Add(this.rightHairArrow);
        }
        if (this.leftHairArrow is not null)
        {
            menu.allClickableComponents.Add(this.leftHairArrow);
        }
    }

    /// <summary>
    /// Updates the filter given the text in the box.
    /// </summary>
    /// <param name="force">Force update even if nothing has changed.</param>
    internal void UpdateForFilter(bool force = false)
    {
        if (!force && this.lastFilter == this.textbox.Text)
        {
            return;
        }

        this.lastFilter = this.textbox.Text;
        this.ShopMenu.applyTab();
        this.ApplyFilter();
    }

    /// <summary>
    /// Applies the current filters the list of items in the dresser-shop. Doesn't actually reset it first.
    /// </summary>
    internal void ApplyFilter()
    {
        if (!string.IsNullOrWhiteSpace(this.textbox.Text) || this.selectedFilter is not null)
        {
            string filter = this.textbox.Text.Trim();
            List<ISalable> filtered = new(this.ShopMenu.forSale.Count);
            foreach (ISalable? item in this.ShopMenu.forSale)
            {
                if ((this.selectedFilter is null || (item is Item actual && this.selectedFilter.Filter(actual)))
                    && (string.IsNullOrEmpty(filter) || item.DisplayName.Contains(filter, StringComparison.InvariantCultureIgnoreCase)))
                {
                    filtered.Add(item);
                }
            }
            this.ShopMenu.forSale = filtered;
        }

        this.ShopMenu.currentItemIndex = Math.Clamp(this.ShopMenu.currentItemIndex, 0, Math.Max(0, this.ShopMenu.forSale.Count - 4));
        this.PerformSort();

        if (Game1.options.snappyMenus && Game1.options.gamepadControls)
        {
            this.snapCursorToCurrentSnappedComponent();
        }
    }

    private static void PlayShwip()
    {
        try
        {
            Game1.playSound("shwip");
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("playing shwip", ex);
        }
    }

    private void PerformSort()
    {
        if (this.status == AlphabetizationStatus.None)
        {
            return;
        }
        StringComparer comparer = AtraUtils.GetCurrentLanguageComparer(true);
        this.ShopMenu.forSale.Sort(new SalableNameComparer(comparer));

        if (this.status == AlphabetizationStatus.Backward)
        {
            this.ShopMenu.forSale.Reverse();
        }
    }

    [MemberNotNull(nameof(portrait))]
    [MemberNotNull(nameof(rotateLeftArrow))]
    [MemberNotNull(nameof(rotateRightArrow))]
    private void AssignClickableComponents()
    {
        this.backdrop = new Rectangle(
            x: this.xPositionOnScreen + (this.width / 2) - 64,
            y: this.yPositionOnScreen + 32,
            width: 128,
            height: 192);
        this.portrait = new(
            x: this.xPositionOnScreen + (this.width / 2) - 32,
            y: this.yPositionOnScreen + 64,
            width: 64,
            height: 96);

        const int ArrowHeight = 44;
        const int ArrowWidth = 48;
        if (ModEntry.Config.HairArrows)
        {
            this.backdrop.Y += 8;
            this.portrait.Y += 8;
            this.leftHairArrow = new(new Rectangle(
                x: this.backdrop.X - 8,
                y: this.backdrop.Y - ArrowHeight + 20,
                width: ArrowWidth,
                height: ArrowHeight),
                Game1.mouseCursors,
                new Rectangle(352, 495, 12, 11),
                3);
            this.rightHairArrow = new(new Rectangle(
                x: this.backdrop.Right - 33 + 8,
                y: this.backdrop.Y - ArrowHeight + 20,
                width: ArrowWidth,
                height: ArrowHeight),
                Game1.mouseCursors,
                new Rectangle(365, 495, 12, 11),
                3);
        }

        // rotation arrows.
        this.rotateLeftArrow = new(new Rectangle(
            x: this.backdrop.X - 8,
            y: this.backdrop.Bottom - ArrowHeight,
            width: ArrowWidth,
            height: ArrowHeight),
            Game1.mouseCursors,
            new Rectangle(352, 495, 12, 11),
            Game1.pixelZoom)
        {
            myID = LEFTARROW,
            leftNeighborID = EQUIPMENT,
            rightNeighborID = RIGHTARROW,
        };
        this.rotateRightArrow = new(new Rectangle(
            x: this.backdrop.Right - ArrowWidth + 8,
            y: this.backdrop.Bottom - ArrowHeight,
            width: ArrowWidth,
            height: ArrowHeight),
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
        this.equipmentIcons.Add(new InventorySlot<Ring>(
            type: EquipmentType.Ring,
            x: this.xPositionOnScreen + 32,
            y: this.yPositionOnScreen + 32,
            name: "Left Ring",
            getItem: static () => Game1.player.leftRing.Value,
            setItem: static (value) => Game1.player.leftRing.Value = value,
            isActive: !blockRingSlots));
        this.equipmentIcons.Add(new InventorySlot<Ring>(
            type: EquipmentType.Ring,
            x: this.xPositionOnScreen + 32,
            y: this.yPositionOnScreen + 32 + 64,
            name: "Right Ring",
            getItem: static () => Game1.player.rightRing.Value,
            setItem: static (value) => Game1.player.rightRing.Value = value,
            isActive: !blockRingSlots));
        this.equipmentIcons.Add(new InventorySlot<Boots>(
            type: EquipmentType.Boots,
            x: this.xPositionOnScreen + 32,
            y: this.yPositionOnScreen + 32 + 128,
            name: "Boots",
            getItem: static () => Game1.player.boots.Value,
            setItem: static value => Game1.player.boots.Value = value));
        this.equipmentIcons.Add(new InventorySlot<Hat>(
            type: EquipmentType.Hat,
            x: this.xPositionOnScreen + this.width - 64 - 32,
            y: this.yPositionOnScreen + 32,
            name: "Hat",
            getItem: static () => Game1.player.hat.Value,
            setItem: static value => Game1.player.hat.Value = value));
        this.equipmentIcons.Add(new ClothingSlot(
            type: EquipmentType.Shirt,
            x: this.xPositionOnScreen + this.width - 64 - 32,
            y: this.yPositionOnScreen + 32 + 64,
            name: "Shirt",
            getItem: static () => Game1.player.shirtItem.Value,
            setItem: static value => Game1.player.shirtItem.Value = value));
        this.equipmentIcons.Add(new ClothingSlot(
            type: EquipmentType.Pants,
            x: this.xPositionOnScreen + this.width - 64 - 32,
            y: this.yPositionOnScreen + 32 + 128,
            name: "Pants",
            getItem: static () => Game1.player.pantsItem.Value,
            setItem: static value => Game1.player.pantsItem.Value = value));

        // hoverbar.
        this.floating = new(this.xPositionOnScreen + this.width + 8, this.yPositionOnScreen + BASEHEIGHT + 8, 720, 80);
        this.textbox.X = this.floating.X + 8;
        this.textbox.Y = this.floating.Y + 16;
        this.textbox.Width = 256;
        this.textbox.Height = 192;
        this.effectiveTextboxArea = new Rectangle(this.textbox.X, this.textbox.Y - 8, this.textbox.Width + 8, 72);

        this.alphabetization = new (
            this.textbox.X + 256 + 32 + 24,
            this.textbox.Y,
            100,
            48);

        int x = this.alphabetization.Right;
        for (int i = 0; i < this.colorFilters.Length; i++)
        {
            int y = this.alphabetization.Y - 16;
            if (i % 2 == 0)
            {
                x += 40;
            }
            else
            {
                y += 40;
            }
            this.colorFilters[i].Reposition(x, y);
        }

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
                        if (i != this.equipmentIcons.Count - 1)
                        {
                            slot.Clickable.downNeighborID = slot.Clickable.myID + 1;
                        }

                        slot.Clickable.upNeighborID = this.ShopMenu.forSaleButtons[^1].myID;
                        break;
                    case 1: // middle
                        if (i != this.equipmentIcons.Count - 1)
                        {
                            slot.Clickable.downNeighborID = slot.Clickable.myID + 1;
                        }

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
                int inventoryID = i % 3;
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

    #region searchbox
    private void OnEntered(TextBox sender)
    {
        this.UpdateForFilter();
    }

    private void OnBackspacePressed(TextBox sender)
    {
        if (!string.IsNullOrEmpty(sender.Text))
        {
            sender.Text = sender.Text[..^1];
        }
        if (string.IsNullOrWhiteSpace(sender.Text))
        {
            this.UpdateForFilter();
        }
    }
    #endregion
}
