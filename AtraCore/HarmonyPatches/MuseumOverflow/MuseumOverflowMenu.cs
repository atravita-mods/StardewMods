// Ignore Spelling: Impl

using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.Menus;

namespace AtraCore.HarmonyPatches.MuseumOverflow;

/// <summary>
/// A little menu to show museum overflow items.
/// </summary>
internal sealed class MuseumOverflowMenu : IClickableMenu
{
    private const int HiddenOffset = -((64 * 3) + 32);
    private const int ShownOffset = 0;

    private const int MAX_BOXEN = 36;
    private const int REGION = 49754;

    #region delegates
    internal static readonly Lazy<Func<MuseumMenu, bool>> _holdingMuseumPieceGetter = new(static () =>
        typeof(MuseumMenu).GetCachedField("holdingMuseumPiece", ReflectionCache.FlagTypes.InstanceFlags)
                          .GetInstanceFieldGetter<MuseumMenu, bool>());

    private static readonly Lazy<Action<MuseumMenu, bool>> _holdingMuseumPieceSetter = new(static () =>
    typeof(MuseumMenu).GetCachedField("holdingMuseumPiece", ReflectionCache.FlagTypes.InstanceFlags)
                      .GetInstanceFieldSetter<MuseumMenu, bool>());
    #endregion

    private readonly MuseumMenu baseMenu;
    private readonly Inventory inventory;

    private State state = State.Hidden;
    private int offset = HiddenOffset;

    // items
    private readonly ClickableComponent[] boxen;

    // buttons
    private readonly ClickableTextureComponent dresserButton;
    private readonly ClickableTextureComponent donateAll;

    // scrolling
    private readonly ClickableTextureComponent upArrow;
    private readonly ClickableTextureComponent downArrow;
    private readonly ClickableComponent scrollBar;
    private Rectangle scrollBarRunner;
    private bool scrolling;
    private int row = 0;

    private Item? hoverItem;
    private string hoverText = string.Empty;

    // sparkles
    private SparklingText? sparkleText;
    private Vector2 locationOfSparkleText;

    /// <summary>
    /// Initializes a new instance of the <see cref="MuseumOverflowMenu"/> class.
    /// </summary>
    /// <param name="menu">The museum menu.</param>
    /// <param name="inventory">The backing inventory.</param>
    public MuseumOverflowMenu(MuseumMenu menu, Inventory inventory)
        : base(0, 0, 64 * 12, (64 * 3) + 16)
    {
        this.baseMenu = menu;
        this.inventory = inventory;

        this.boxen = new ClickableComponent[MAX_BOXEN];
        for (int i = 0; i < this.boxen.Length; i++)
        {
            (int y, int x) = Math.DivRem(i, 12);
            this.boxen[i] = new ClickableComponent(
                new Rectangle(
                    x * 64,
                    y * 64,
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

        this.xPositionOnScreen = ((Game1.uiViewport.Width - this.width) / 2) - 24;
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
        this.scrollBar = new(new Rectangle(this.upArrow.bounds.X + 12, this.upArrow.bounds.Y + this.upArrow.bounds.Height + 4, 24, 40), "scrollbar");
        this.donateAll = new(
            "DonateAll",
            new Rectangle(this.xPositionOnScreen - 128, this.yPositionOnScreen + 128, 64, 64),
            string.Empty,
            I18n.DonateAll(),
            Game1.mouseCursors,
            new Rectangle(103, 469, 16, 16),
            Game1.pixelZoom)
        {
            myID = 500,
        };

        this.dresserButton = new ClickableTextureComponent(
            new Rectangle(this.xPositionOnScreen - 128, this.yPositionOnScreen, 64, 64),
            texture: Game1.content.Load<Texture2D>("Tilesheets/furniture"),
            new Rectangle(0, 352, 32, 32),
            2f);

        this.Reposition(Game1.uiViewport.ToXNARectangle());
    }

    private enum State
    {
        Hidden,
        Extending,
        Extended,
        Retracting,
    }

    /// <summary>
    /// Gets the maximum number of rows.
    /// </summary>
    internal int MaxRows => (int)Math.Ceiling(this.inventory.Count / 12f);

    /// <inheritdoc />
    public override void performHoverAction(int x, int y)
    {
        this.upArrow.tryHover(x, y);
        this.downArrow.tryHover(x, y);
        this.dresserButton.tryHover(x, y);
        this.donateAll.tryHover(x, y);

        base.performHoverAction(x, y);
        if (this.donateAll.containsPoint(x, y))
        {
            this.hoverText = this.donateAll.hoverText;
            this.hoverItem = null;
            return;
        }

        // offset to match where these boxes "actually" are.
        x -= this.xPositionOnScreen;
        y -= this.yPositionOnScreen + this.offset + 12;

        for (int i = 0; i < this.boxen.Length; i++)
        {
            if (this.boxen[i].containsPoint(x, y))
            {
                Item? item = this.GetItem(i);
                if (!ReferenceEquals(this.hoverItem, item))
                {
                    this.hoverText = item?.getDescription() ?? string.Empty;
                }
                this.hoverItem = item;
                return;
            }
        }

        this.hoverItem = null;
        this.hoverText = string.Empty;
    }

    /// <summary>
    /// Handles a left click.
    /// </summary>
    /// <param name="x">pixel x.</param>
    /// <param name="y">pixel y.</param>
    /// <param name="playSound">whether or not to play sounds.</param>
    /// <returns>true if handled, false otherwise.</returns>
    public bool LeftClickImpl(int x, int y, bool playSound = true)
    {
        this.receiveLeftClick(x, y, playSound);
        if (this.dresserButton.containsPoint(x, y))
        {
            this.state = this.state switch
            {
                State.Extended or State.Extending => State.Retracting,
                _ => State.Extending,
            };

            return true;
        }

        if (this.state != State.Extended || this.baseMenu.fadeTimer > 0)
        {
            return false;
        }

        if (this.scrollBar.containsPoint(x, y))
        {
            this.scrolling = true;
            this.leftClickHeld(x, y);
            return true;
        }
        else if (this.scrollBarRunner.Contains(new Point(x, y)))
        {
            this.SetScrollToPosition(y - (this.scrollBar.bounds.Height / 2));
            return true;
        }

        if (this.upArrow.containsPoint(x, y))
        {
            this.row = Math.Max(0, this.row - 1);
            this.upArrow.scale = this.upArrow.baseScale;
            this.SetScrollBarToCurrentIndex();
            Play("shwip");
            return true;
        }

        if (this.downArrow.containsPoint(x, y))
        {
            int maxRow = Math.Max(0, this.MaxRows - 2);
            this.downArrow.scale = this.downArrow.baseScale;
            this.row = Math.Min(this.row + 1, maxRow);
            this.SetScrollBarToCurrentIndex();
            Play("shwip");
            return true;
        }

        if (this.baseMenu.heldItem is null && this.donateAll.containsPoint(x, y))
        {
            if (Game1.currentLocation is not LibraryMuseum library)
            {
                return false;
            }

            Inventory playerInventory = Game1.player.Items;
            int lastRewardsCount = library.getRewardsForPlayer(Game1.player).Count;
            int prevPieces = library.museumPieces.Length + this.inventory.Count;
            for (int i = 0; i < playerInventory.Count; i++)
            {
                Item proposed = playerInventory[i];
                if (!this.baseMenu.inventory.highlightMethod(proposed) || !library.isItemSuitableForDonation(proposed))
                {
                    continue;
                }

                this.inventory.Add(proposed.getOne());
                proposed.Stack--;
                if (proposed.Stack <= 0)
                {
                    playerInventory[i] = null;
                }

                Game1.Multiplayer.globalChatInfoMessage("donation", Game1.player.Name, "object:" + proposed.ItemId);
            }

            int rewardsCount = library.getRewardsForPlayer(Game1.player).Count;
            int currentPieces = library.museumPieces.Length + this.inventory.Count;
            if (currentPieces == LibraryMuseum.totalArtifacts)
            {
                Game1.Multiplayer.globalChatInfoMessage("MuseumComplete", Game1.player.farmName.Value);
            }
            else if (prevPieces < 40 && currentPieces >= 40)
            {
                Game1.Multiplayer.globalChatInfoMessage("Museum40", Game1.player.farmName.Value);
            }

            if (rewardsCount > lastRewardsCount)
            {
                this.sparkleText = new SparklingText(Game1.dialogueFont, Game1.content.LoadString("Strings\\StringsFromCSFiles:NewReward"), Color.MediumSpringGreen, Color.White);
                Play("reward");
                this.locationOfSparkleText = new(this.xPositionOnScreen + (this.width / 2) - (this.sparkleText.textWidth / 2), this.yPositionOnScreen + (this.height / 2) - 12);
            }
            else
            {
                Play("newArtifact");
            }

            Game1.player.completeQuest("24");
            Game1.stats.checkForArchaeologyAchievements();

            int maxRow = this.MaxRows;
            this.row = Math.Max(0, maxRow - 2);
            this.SetScrollBarToCurrentIndex();
            return true;
        }

        // offset to match where these boxes "actually" are.
        x -= this.xPositionOnScreen;
        y -= this.yPositionOnScreen + this.offset + 12;

        // extracted from MuseumMenu.recieveLeftClick
        for (int i = 0; i < this.boxen.Length; i++)
        {
            ClickableComponent box = this.boxen[i];
            if (!box.containsPoint(x, y))
            {
                continue;
            }

            if (this.baseMenu.heldItem is { } held)
            {
                if (Game1.currentLocation is LibraryMuseum library && library.isItemSuitableForDonation(held))
                {
                    bool holdingMuseumPiece = _holdingMuseumPieceGetter.Value(this.baseMenu);
                    int lastRewardsCount = !holdingMuseumPiece ? library.getRewardsForPlayer(Game1.player).Count : 0;
                    this.inventory.Add(held.getOne());
                    Play("stoneStep");

                    int idxOfAddedItem = this.inventory.Count - 1 - (this.row * 12);
                    if (idxOfAddedItem >= 36)
                    {
                        this.row++;
                        idxOfAddedItem -= 12;
                        this.SetScrollBarToCurrentIndex();
                    }

                    if (!holdingMuseumPiece && library.getRewardsForPlayer(Game1.player).Count > lastRewardsCount)
                    {
                        this.sparkleText = new SparklingText(Game1.dialogueFont, Game1.content.LoadString("Strings\\StringsFromCSFiles:NewReward"), Color.MediumSpringGreen, Color.White);
                        Play("reward");
                        ClickableComponent boxToShow = this.boxen[Math.Clamp(idxOfAddedItem, 0, MAX_BOXEN)];
                        this.locationOfSparkleText = new Vector2(boxToShow.bounds.X + 32 - (this.sparkleText.textWidth / 2) + this.xPositionOnScreen, boxToShow.bounds.Y - 12 + this.yPositionOnScreen + this.offset);
                    }
                    else
                    {
                        Play("newArtifact");
                    }

                    Game1.player.completeQuest("24");
                    this.baseMenu.heldItem.Stack--;
                    if (this.baseMenu.heldItem.Stack <= 0)
                    {
                        this.baseMenu.heldItem = null;
                    }

                    if (!holdingMuseumPiece)
                    {
                        // broadcast multiplayer messages.
                        int pieces = library.museumPieces.Length + this.inventory.Count;
                        Game1.stats.checkForArchaeologyAchievements();
                        if (pieces == LibraryMuseum.totalArtifacts)
                        {
                            Game1.Multiplayer.globalChatInfoMessage("MuseumComplete", Game1.player.farmName.Value);
                        }
                        else if (pieces == 40)
                        {
                            Game1.Multiplayer.globalChatInfoMessage("Museum40", Game1.player.farmName.Value);
                        }
                        else
                        {
                            Game1.Multiplayer.globalChatInfoMessage("donation", Game1.player.Name, "object:" + held.ItemId);
                        }
                    }
                    this.baseMenu.ReturnToDonatableItems();

                    return true;
                }
            }

            if (this.GetItem(i) is Item item && this.baseMenu.heldItem is null && Game1.player.couldInventoryAcceptThisItem(item))
            {
                this.baseMenu.heldItem = item;
                _holdingMuseumPieceSetter.Value(this.baseMenu, true);
                this.inventory.Remove(item);

                this.baseMenu.menuMovingDown = true;
                this.baseMenu.reOrganizing = false; // ?
            }
            return true;
        }

        return false;

        void Play(string sound)
        {
            if (playSound)
            {
                Game1.playSound(sound);
            }
        }
    }

    /// <inheritdoc />
    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);
        if (this.scrolling)
        {
            this.SetScrollToPosition(y - (this.scrollBar.bounds.Height / 2));
        }
    }

    /// <inheritdoc />
    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);
        this.scrolling = false;
    }

    /// <inheritdoc />
    public override void draw(SpriteBatch b)
    {
        if ((this.baseMenu.fadeTimer > 0 && this.baseMenu.fadeIntoBlack) || this.baseMenu.state == 3)
        {
            return;
        }

        // box for dresser
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
            this.height + vpadding,
            Color.White,
            Game1.pixelZoom);

        if (this.state != State.Hidden)
        {
            // slots
            Rectangle inventoryBox = Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10);
            for (int i = 0; i < this.boxen.Length; i++)
            {
                ClickableComponent box = this.boxen[i];
                Rectangle bounds = box.bounds;
                Vector2 location = new(this.xPositionOnScreen + bounds.X, this.yPositionOnScreen + bounds.Y + this.offset + 12);
                b.Draw(
                    Game1.menuTexture,
                    location,
                    inventoryBox,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    1f);

                // item
                if (this.GetItem(i) is Item item)
                {
                    item.drawInMenu(b, location, box.scale);
                }
            }
        }

        if (this.state == State.Extended)
        {
            this.donateAll.draw(b);

            drawTextureBox(
                b,
                Game1.mouseCursors,
                new Rectangle(403, 383, 6, 6),
                this.scrollBarRunner.X,
                this.scrollBarRunner.Y,
                this.scrollBarRunner.Width,
                this.scrollBarRunner.Height,
                Color.White,
                4f);
            this.upArrow.draw(b);
            this.downArrow.draw(b);

            // draw the scrollbar
            const int sourceX = 435;
            const int sourceY = 463;
            const int sourceWidth = 6;
            // height is 10
            Rectangle bounds = this.scrollBar.bounds;

            // draw the middle of the scrollbar, the middle pixels.
            b.Draw(
                Game1.mouseCursors,
                new Rectangle(bounds.X, bounds.Y + 8, bounds.Width, bounds.Height - 16),
                new Rectangle(sourceX, sourceY + 2, sourceWidth, 6),
                Color.White);

            // draw the top of the scrollbar, the top eight pixels.
            b.Draw(
                Game1.mouseCursors,
                new Vector2(bounds.X, bounds.Y),
                new Rectangle(sourceX, sourceY, sourceWidth, 2),
                Color.White,
                0f,
                Vector2.Zero,
                Game1.pixelZoom,
                SpriteEffects.None,
                0);

            // draw the bottom of the scrollbar, the bottom eight pixels.
            b.Draw(
                Game1.mouseCursors,
                new Vector2(bounds.X, bounds.Bottom - 8),
                new Rectangle(sourceX, sourceY + 8, sourceWidth, 2),
                Color.White,
                0f,
                Vector2.Zero,
                Game1.pixelZoom,
                SpriteEffects.None,
                0);

            this.sparkleText?.draw(b, this.locationOfSparkleText);

            if (!string.IsNullOrEmpty(this.hoverText))
            {
                drawHoverText(b, this.hoverText, Game1.smallFont);
            }
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
                this.offset += 4;
                if (this.offset >= ShownOffset)
                {
                    this.offset = ShownOffset;
                    this.state = State.Extended;
                }
                break;
            }
            case State.Retracting:
            {
                this.offset -= 4;
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

        if (this.sparkleText?.update(time) == true)
        {
            this.sparkleText = null;
        }
    }

    /// <inheritdoc />
    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds) => this.Reposition(Game1.uiViewport.ToXNARectangle());

    private void Reposition(Rectangle newBounds)
    {
        this.xPositionOnScreen = ((newBounds.Width - this.width) / 2) - 24;
        this.yPositionOnScreen = 20;

        this.upArrow.setPosition(new(this.xPositionOnScreen + this.width + 32, this.yPositionOnScreen));
        this.downArrow.setPosition(new(this.xPositionOnScreen + this.width + 32, this.yPositionOnScreen + this.height - 48));

        this.dresserButton.setPosition(new(this.xPositionOnScreen - 128, this.yPositionOnScreen - 20));
        this.donateAll.setPosition(new(this.xPositionOnScreen - 128, this.yPositionOnScreen + 128));

        this.scrollBarRunner = new Rectangle(
            this.scrollBar.bounds.X,
            this.upArrow.bounds.Bottom + 4,
            this.scrollBar.bounds.Width,
            this.downArrow.bounds.Y - this.upArrow.bounds.Bottom - 12);

        this.SetScrollBarToCurrentIndex();
    }

    private void SetScrollToPosition(int y)
    {
        int prev = this.row;
        y = Math.Clamp(y, this.scrollBarRunner.Y, this.scrollBarRunner.Y + this.scrollBarRunner.Height - this.scrollBar.bounds.Height);
        int maxRows = Math.Max(this.MaxRows - 2, 0);

        int total = this.scrollBarRunner.Height - this.scrollBar.bounds.Height;
        float percentage = (y - this.scrollBarRunner.Y) * 1f / total;

        this.row = Math.Clamp((int)Math.Floor(percentage * maxRows), 0, maxRows);
        if (prev != this.row)
        {
            Game1.playSound("shiny4");
        }
        this.SetScrollBarToCurrentIndex();
    }

    /// <summary>
    /// Sets the scrollbar to the current index.
    /// </summary>
    private void SetScrollBarToCurrentIndex()
    {
        float maxRows = Math.Max(this.MaxRows - 2, 0);
        this.scrollBar.bounds.Height = (int)Math.Floor(24f / (maxRows + 1)) + 24;

        float fraction = (maxRows == 0) ? 0 : this.row / maxRows;
        float offset = fraction * (this.scrollBarRunner.Height - this.scrollBar.bounds.Height);

        this.scrollBar.bounds.Y = this.scrollBarRunner.Y + (int)offset;
    }

    private Item? GetItem(int i)
    {
        int index = (this.row * 12) + i;
        if (index < this.inventory.Count)
        {
            return this.inventory[index];
        }
        return null;
    }
}

/// <summary>
/// Extensions for this class.
/// </summary>
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1400:Access modifier should be declared", Justification = "file is a valid access modifier.")]
file static class Extensions
{
    /// <summary>
    /// Converts an xTile rectangle to an XNA rectangle.
    /// </summary>
    /// <param name="original">xtile rectangle.</param>
    /// <returns>XNA rectangle.</returns>
    internal static Rectangle ToXNARectangle(this xTile.Dimensions.Rectangle original)
        => new(original.X, original.Y, original.Width, original.Height);
}