using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Menus;

namespace StopRugRemoval.Framework.Menus;

/// <summary>
/// A little mini menu for farmer customization.
/// </summary>
internal sealed class MiniFarmerMenu : IClickableMenu
{
    private readonly List<ClickableComponent> equipmentIcons = new();
    private ClickableComponent portrait;
    private Rectangle backdrop;

    internal ShopMenu shopMenu { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MiniFarmerMenu"/> class.
    /// </summary>
    /// <param name="shopMenu">ShopMenu to hang out with.</param>
    internal MiniFarmerMenu(ShopMenu shopMenu)
        : base(shopMenu.xPositionOnScreen - 128, shopMenu.yPositionOnScreen + 384 + 64 + 32, 384, 256)
    {
        this.shopMenu = shopMenu;
        this.AssignClickableComponents();
    }

    public override void draw(SpriteBatch b)
    {
        // draw backdrop
        drawTextureBox(b, this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, Color.White);

        // draw equipment icons
        foreach (var com in this.equipmentIcons)
        {
            b.Draw(Game1.staminaRect, com.bounds, Color.LimeGreen);
        }

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
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        => base.gameWindowSizeChanged(oldBounds, newBounds);

    [MemberNotNull(nameof(portrait))]
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

        // equipment icons.
        this.equipmentIcons.Clear();
        this.equipmentIcons.Add(new ClickableComponent(
            new Rectangle(
            x: this.xPositionOnScreen + 32,
            y: this.yPositionOnScreen + 32,
            width: 64,
            height: 64), "Left Ring")
        {
            myID = InventoryPage.region_ring1,
            downNeighborID = InventoryPage.region_ring2,
            rightNeighborID = InventoryPage.region_hat,
        });
        this.equipmentIcons.Add(new ClickableComponent(
            new Rectangle(
            x: this.xPositionOnScreen + 32,
            y: this.yPositionOnScreen + 32 + 64,
            width: 64,
            height: 64), "Right Ring")
        {
            myID = 103,
            upNeighborID = 102,
            downNeighborID = 104,
            rightNeighborID = 108,
        });
        this.equipmentIcons.Add(new ClickableComponent(
            new Rectangle(
            x: this.xPositionOnScreen + 32,
            y: this.yPositionOnScreen + 32 + 128,
            width: 64,
            height: 64), "Boots")
        {
            myID = 104,
            upNeighborID = 103,
            rightNeighborID = 109,
        });
        this.equipmentIcons.Add(new ClickableComponent(
            new Rectangle(
            x: this.xPositionOnScreen + this.width - 64 - 32,
            y: this.yPositionOnScreen + 32,
            width: 64,
            height: 64), "Hat")
        {
            myID = 101,
            leftNeighborID = 102,
            downNeighborID = 108,
            upNeighborID = Game1.player.MaxItems - 12,
            rightNeighborID = 105,
        });
        this.equipmentIcons.Add(new ClickableComponent(
            new Rectangle(
            x: this.xPositionOnScreen + this.width - 64 - 32,
            y: this.yPositionOnScreen + 32 + 64,
            width: 64,
            height: 64), "Shirt")
        {
            myID = 108,
            upNeighborID = 101,
            downNeighborID = 109,
            rightNeighborID = 105,
            leftNeighborID = 103,
        });
        this.equipmentIcons.Add(new ClickableComponent(
            new Rectangle(
            x: this.xPositionOnScreen + this.width - 64 - 32,
            y: this.yPositionOnScreen + 32 + 128,
            width: 64,
            height: 64), "Pants")
        {
            myID = 109,
            upNeighborID = 108,
            rightNeighborID = 105,
            leftNeighborID = 104,
        });
    }
}
