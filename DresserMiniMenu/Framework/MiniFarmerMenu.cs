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
    private readonly List<IInventorySlot<Item>> equipmentIcons = new();
    private ClickableComponent portrait;
    private Rectangle backdrop;

    #region hover
    private Item? hoverItem;
    private string? hoverText;
    private string? hoverTitle;
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="MiniFarmerMenu"/> class.
    /// </summary>
    /// <param name="shopMenu">ShopMenu to hang out with.</param>
    internal MiniFarmerMenu(ShopMenu shopMenu)
        : base(shopMenu.xPositionOnScreen - 128, shopMenu.yPositionOnScreen + 480 - 16, 384, 256 - 4)
    {
        this.ShopMenu = shopMenu;
        this.AssignClickableComponents();
    }

    /// <summary>
    /// Gets the shop (dresser) menu associated with this instance.
    /// </summary>
    internal ShopMenu ShopMenu { get; init; }

    /// <inheritdoc />
    public override void performHoverAction(int x, int y)
    {
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
        Game1.player.FarmerRenderer.draw(
            b,
            new FarmerSprite.AnimationFrame(0, Game1.player.bathingClothes.Value ? 108 : 0, secondaryArm: false, flip: false),
            Game1.player.bathingClothes.Value ? 108 : 0,
            new Rectangle(0, Game1.player.bathingClothes.Value ? 576 : 0, 16, 32),
            new Vector2(this.portrait.bounds.X, this.portrait.bounds.Y),
            Vector2.Zero,
            0.8f,
            Game1.down,
            Color.White,
            0f,
            1f,
            Game1.player);
        if (isDarkOut)
        {
            Game1.player.FarmerRenderer.draw(
            b,
            new FarmerSprite.AnimationFrame(0, Game1.player.bathingClothes.Value ? 108 : 0, secondaryArm: false, flip: false),
            Game1.player.bathingClothes.Value ? 108 : 0,
            new Rectangle(0, Game1.player.bathingClothes.Value ? 576 : 0, 16, 32),
            new Vector2(this.portrait.bounds.X, this.portrait.bounds.Y),
            Vector2.Zero,
            0.8f,
            Game1.down,
            Color.DarkBlue * 0.3f,
            0f,
            1f,
            Game1.player);
        }
        FarmerRenderer.isDrawingForUI = false;

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

        base.receiveLeftClick(x, y, playSound);
    }

    [MemberNotNull(nameof(portrait))]
    private void AssignClickableComponents()
    {
        this.backdrop = new Rectangle(
            x: this.xPositionOnScreen + this.width / 2 - 64,
            y: this.yPositionOnScreen + 32,
            width: 128,
            height: 192);
        this.portrait = new ClickableComponent(
            new Rectangle(
            x: this.xPositionOnScreen + this.width / 2 - 32,
            y: this.yPositionOnScreen + 64,
            width: 64,
            height: 96),
            name: "Portrait");

        // equipment icons.
        this.equipmentIcons.Clear();

        this.equipmentIcons.Add(new RingSlot(
            x: this.xPositionOnScreen + 32,
            y: this.yPositionOnScreen + 32,
            name: "Left Ring",
            getItem: static () => Game1.player.leftRing.Value,
            setItem: static (value) => Game1.player.leftRing.Value = value));
        this.equipmentIcons.Add(new RingSlot(
            x: this.xPositionOnScreen + 32,
            y: this.yPositionOnScreen + 32 + 64,
            name: "Right Ring",
            getItem: static () => Game1.player.rightRing.Value,
            setItem: static (value) => Game1.player.rightRing.Value = value));
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
    }
}
