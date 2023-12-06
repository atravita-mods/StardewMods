using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Inventories;
using StardewValley.Menus;

namespace AtraCore.HarmonyPatches.MuseumOverflow;

/// <summary>
/// A little menu to show museum overflow items.
/// </summary>
internal sealed class MuseumOverflowMenu : IClickableMenu
{
    private const int HiddenOffset = -((64 * 3) + 32);
    private const int ShownOffset = 0;
    private readonly MuseumMenu baseMenu;
    private readonly Inventory inventory;

    private State state = State.Hidden;
    private int offset = HiddenOffset;

    // items
    private const int MAX_BOXEN = 36;
    private const int REGION = 49754;
    private readonly ClickableComponent[] boxen;

    // scrolling
    private Rectangle scrollBarRunner;
    private ClickableTextureComponent upArrow;
    private ClickableTextureComponent downArrow;
    private ClickableTextureComponent scrollBar;
    private int row = 0;

    private ClickableTextureComponent dresserButton;

    /// <summary>
    /// Initializes a new instance of the <see cref="MuseumOverflowMenu"/> class.
    /// </summary>
    /// <param name="menu">The museum menu.</param>
    /// <param name="inventory">The backing inventory.</param>
    public MuseumOverflowMenu(MuseumMenu menu, Inventory inventory)
        : base(0, 0, 64 * 12 , (64 * 3) + 16)
    {
        this.baseMenu = menu;
        this.inventory = inventory;

        this.boxen = new ClickableComponent[MAX_BOXEN];
        for (int i = 0; i < this.boxen.Length; i++)
        {
            (int y, int x) = Math.DivRem(i, 12);
            this.boxen[i] = new ClickableComponent(
                new Rectangle(
                    this.xPositionOnScreen + (x * 64),
                    this.yPositionOnScreen + (y * 64),
                    64,
                    64),
                i.ToString() ?? string.Empty)
            {
                myID = i,
                leftNeighborID = (x != 0) ? (i - 1) : 107,
                rightNeighborID = (x != 11) ? (i + 1) : 106,
                downNeighborID = (y == 2) ? (12340 + i) : (i + 12),
                upNeighborID = (y == 0) ? 102 : (i - 12),
                region = REGION,
                upNeighborImmutable = true,
                downNeighborImmutable = true,
                leftNeighborImmutable = true,
                rightNeighborImmutable = true,
            };
        }

        this.xPositionOnScreen = (Game1.uiViewport.Width - this.width) / 2;
        this.yPositionOnScreen = 20;

        this.upArrow = new ClickableTextureComponent(
            new Rectangle(this.xPositionOnScreen + this.width + 32, this.yPositionOnScreen + 16, 44, 48),
            Game1.mouseCursors,
            new Rectangle(421, 459, 11, 12),
            Game1.pixelZoom);
        this.downArrow = new ClickableTextureComponent(
            new Rectangle(this.xPositionOnScreen + this.width + 32, this.yPositionOnScreen + this.height - 64, 44, 48),
            Game1.mouseCursors,
            new Rectangle(421, 472, 11, 12),
            Game1.pixelZoom);
        this.scrollBar = new ClickableTextureComponent(
            new Rectangle(this.upArrow.bounds.X + 12, this.upArrow.bounds.Y + this.upArrow.bounds.Height + 4, 24, 40),
            Game1.mouseCursors,
            new Rectangle(435, 463, 6, 10),
            Game1.pixelZoom);

        this.dresserButton = new ClickableTextureComponent(
            new Rectangle(this.xPositionOnScreen - 128, this.yPositionOnScreen, 64, 64),
            texture: Game1.content.Load<Texture2D>("Tilesheets/furniture"),
            new Rectangle(0, 352, 32, 32),
            2f);
        this.scrollBarRunner = new Rectangle(this.scrollBar.bounds.X, this.upArrow.bounds.Y + this.upArrow.bounds.Height + 4, this.scrollBar.bounds.Width, this.height - 64 - this.upArrow.bounds.Height - 28);

        this.Reposition(Game1.uiViewport.ToXNARectangle());
    }

    /// <inheritdoc />
    public override void performHoverAction(int x, int y)
    {
        this.upArrow.tryHover(x, y);
        this.downArrow.tryHover(x, y);
        this.dresserButton.tryHover(x, y);
        base.performHoverAction(x, y);
    }

    /// <inheritdoc />
    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (this.dresserButton.containsPoint(x, y))
        {
            this.state = this.state switch
            {
                State.Hidden or State.Extending => State.Retracting,
                _ => State.Extending,
            };
        }

        if (this.state != State.Extended)
        {
            return;
        }
    }

    /// <inheritdoc />
    public override void draw(SpriteBatch b)
    {
        if ((this.baseMenu.fadeTimer > 0 && this.baseMenu.fadeIntoBlack) || this.baseMenu.state == 3)
        {
            return;
        }

        //box for dresser
        drawTextureBox(
            b,
            Game1.mouseCursors,
            new Rectangle(384, 373, 18, 18),
            this.dresserButton.bounds.X - 20,
            this.dresserButton.bounds.Y - 20,
            this.dresserButton.bounds.Width + 40,
            this.dresserButton.bounds.Height + 40,
            Color.White,
            Game1.pixelZoom
            );
        this.dresserButton.draw(b);

        const int hpadding = 64;
        const int vpadding = 48;
        drawTextureBox(
            b,
            Game1.mouseCursors,
            new Rectangle(384, 373, 18, 18),
            this.xPositionOnScreen - (hpadding / 2),
            this.yPositionOnScreen - (vpadding / 2) + this.offset,
            this.width + hpadding,
            this.height + vpadding - 4,
            Color.White,
            Game1.pixelZoom);

        // slots
        Rectangle inventoryBox = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10);
        foreach (ClickableComponent box in this.boxen)
        {
            Rectangle bounds = box.bounds;
            b.Draw(
                Game1.menuTexture,
                new Vector2(bounds.X, bounds.Y + this.offset),
                inventoryBox,
                Color.White,
                0f,
                Vector2.Zero,
                1f,
                SpriteEffects.None,
                1f);
        }

        // items in slots.

        if (this.state == State.Extended)
        {
            this.upArrow.draw(b);
            this.downArrow.draw(b);

            this.scrollBar.draw(b);
        }
    }

    /// <inheritdoc />
    public override void update(GameTime time)
    {
        base.update(time);

        switch (this.state)
        {
            case State.Extending:
            {
                this.offset += 2;
                if (this.offset >= ShownOffset)
                {
                    this.offset = ShownOffset;
                    this.state = State.Extended;
                }
                break;
            }
            case State.Retracting:
            {
                this.offset -= 2;
                if (this.offset <= HiddenOffset)
                {
                    this.offset = HiddenOffset;
                    this.state = State.Hidden;
                }
                break;
            }
            default:
                break;
        }
    }

    /// <inheritdoc />
    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => this.Reposition(Game1.uiViewport.ToXNARectangle());

    private void Reposition(Rectangle newBounds)
    {
        this.xPositionOnScreen = (newBounds.Width - this.width) / 2;
        this.yPositionOnScreen = 20;

        this.upArrow.setPosition(new(this.xPositionOnScreen + this.width + 32, this.yPositionOnScreen + 16));
        this.downArrow.setPosition(new(this.xPositionOnScreen + this.width + 32, this.yPositionOnScreen + this.height - 64));

        this.scrollBar.setPosition(new(this.upArrow.bounds.X + 12, this.upArrow.bounds.Y + this.upArrow.bounds.Height + 4));
        this.dresserButton.setPosition(new(this.xPositionOnScreen - 128, this.yPositionOnScreen - 20));
    }

    private enum State
    {
        Hidden,
        Extending,
        Extended,
        Retracting,
    }
}

/// <summary>
/// Extensions for this class.
/// </summary>
file static class Extensions
{
    internal static Rectangle ToXNARectangle(this xTile.Dimensions.Rectangle original)
        => new(original.X, original.Y, original.Width, original.Height);
}