using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Delegates;
using StardewValley.TokenizableStrings;
using StardewValley.Triggers;

namespace AtraCore.Framework.Triggers;

/// <summary>
/// The shop item trigger item class.
/// </summary>
public class ShopItemTrigger : Item
{
    private readonly string? _displayNameFormat;
    private readonly string? _descriptionFormat;

    private List<CachedAction>? actions;
    private string? _displayName;
    private string? _description;

    private readonly Texture2D texture;
    private readonly Rectangle sourceRect;

    public int Price { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShopItemTrigger"/> class.
    /// </summary>
    /// <param name="itemId">The item id to use.</param>
    public ShopItemTrigger(string itemId)
    {
        this.ItemId = itemId;

        try
        {
            ShopItemTriggerModel? data = AssetManager.GetShopItemTriggers().GetValueOrDefault(itemId);
            if (data is not null)
            {
                this.texture = Game1.content.Load<Texture2D>(data.Texture);
                this.sourceRect = data.SourceRect;
                this.Price = data.Price;
                this._displayNameFormat = data.DisplayName;
                this._descriptionFormat = data.Description;

                if (data.Actions is { } actions)
                {
                    this.actions = new(actions.Count);
                    foreach (string action in actions)
                    {
                        string[] splits = ArgUtility.SplitBySpaceQuoteAware(action);
                        if (splits.Length == 0)
                        {
                            continue;
                        }

                        string key = splits[0];
                        if (!TriggerActionManager.TryGetActionHandler(key, out TriggerActionDelegate? handler))
                        {
                            ModEntry.ModMonitor.Log($"Failed parsing {action} - no matching action found - for {itemId}.", LogLevel.Warn);
                            continue;
                        }

                        this.actions.Add(new(splits, handler, null, false));
                    }
                }

                return;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"creating {itemId}", ex);
        }

        this.texture = Game1.mouseCursors;
        this.sourceRect = new Rectangle(320, 496, 16, 16);
    }

    /// <inheritdoc />
    public override string TypeDefinitionId { get; } = "atravita.ShopItem";

    /// <inheritdoc />
    public override string DisplayName
    {
        get => this._displayName ??= TokenParser.ParseText(this._displayNameFormat) ?? "Error - no text.";
    }

    /// <inheritdoc />
    public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow) => throw new NotImplementedException();

    /// <inheritdoc />
    public override string getDescription() => this._description ??= Game1.parseText(TokenParser.ParseText(this._descriptionFormat) ?? "No Text Found", Game1.smallFont, this.getDescriptionWidth());

    /// <inheritdoc />
    public override bool isPlaceable() => false;

    /// <inheritdoc />
    public override int maximumStackSize() => 1;

    /// <inheritdoc />
    public override bool actionWhenPurchased(string shopId)
    {
        if (!base.actionWhenPurchased(shopId) && this.actions is not null)
        {
            foreach (var action in this.actions)
            {
                // action.Handler.Invoke(actions, shopId);
            }
        }

        return true;
    }

    /// <inheritdoc />
    protected override Item GetOneNew() => new ShopItemTrigger(this.ItemId)
    {
        actions = this.actions,
    };
}
