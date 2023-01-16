﻿using System.Xml.Serialization;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace GrowableBushes.Framework;

/// <summary>
/// A bush in the inventory.
/// </summary>
[XmlType("Mods_atravita_InventoryBush")]
public sealed class InventoryBush : SObject
{
    [XmlIgnore]
    private Rectangle sourceRect = default;

    /// <summary>
    /// The prefix used for the internal name of these bushes.
    /// </summary>
    internal const string BushPrefix = "atravita.InventoryBush.";

    /// <summary>
    /// The moddata string used to mark the bushes planted with this mod.
    /// </summary>
    internal const string BushModData = "atravita.InventoryBush.Type";

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryBush"/> class.
    /// Constructor for the serializer.
    /// </summary>
    public InventoryBush() : base() {}

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryBush"/> class.
    /// </summary>
    /// <param name="whichBush">Which bush this inventory bush corresponds to.</param>
    /// <param name="initialStack">Initial stack of bushes.</param>
    public InventoryBush(BushSizes whichBush, int initialStack)
        : base((int)whichBush, initialStack, false, -1, 0)
    {
        if (!BushSizesExtensions.IsDefined(whichBush))
        {
            ModEntry.ModMonitor.Log($"Bush {whichBush.ToStringFast()} doesn't seem to be a valid bush? Setting as smol bush.", LogLevel.Error);
            this.ParentSheetIndex = (int)BushSizes.Small;
        }

        this.bigCraftable.Value = true;
        this.CanBeSetDown = true;
        this.Name = BushPrefix + ((BushSizes)this.ParentSheetIndex).ToStringFast();
        this.Edibility = inedible;
        this.Price = 0;
        this.Category = -15500057; // random negative integer.

        // just to make sure the bush texture is loaded.
        _ = Bush.texture.Value;
    }

    #region reflection

    /// <summary>
    /// Stardew's Bush::shake.
    /// </summary>
    private static readonly BushShakeDel BushShakeMethod = typeof(Bush)
        .GetCachedMethod("shake", ReflectionCache.FlagTypes.InstanceFlags)
        .CreateDelegate<BushShakeDel>();

    private delegate void BushShakeDel(
        Bush bush,
        Vector2 tileLocation,
        bool doEvenIfStillShaking);

    #endregion

    #region placement

    /// <inheritdoc />
    public override bool canBePlacedHere(GameLocation l, Vector2 tile)
    {
        int width = ((BushSizes)this.ParentSheetIndex).GetWidth();

        for (int y = (int)tile.Y; y < (int)tile.Y + width; y++)
        {
            if (!IsTilePlaceableForBush(l, (int)tile.X, y))
            {
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc />
    public override bool placementAction(GameLocation location, int x, int y, Farmer? who = null)
    {
        BushSizes size = (BushSizes)this.ParentSheetIndex;

        Vector2 placementTile = new (x / Game1.tileSize, y / Game1.tileSize);
        Bush bush = new (placementTile, size.ToStardewBush(), location);

        // set metadata.
        switch (size)
        {
            case BushSizes.SmallAlt:
                bush.tileSheetOffset.Value = 1;
                break;
            case BushSizes.Harvested:
                bush.tileSheetOffset.Value = 0;
                break;
            case BushSizes.Medium:
            case BushSizes.Large:
                bush.townBush.Value = false;
                break;
            case BushSizes.Town:
            case BushSizes.TownLarge:
                bush.townBush.Value = true;
                break;
        }

        bush.setUpSourceRect();
        bush.modData.SetEnum(BushModData, size);
        location.largeTerrainFeatures.Add(bush);
        location.playSound("thudStep");
        BushShakeMethod(bush, placementTile, true);
        return true;
    }

    #endregion

    #region draw

    /// <inheritdoc />
    public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1)
    {
        float draw_layer = Math.Max(
            0f,
            ((y * 64) + 40) / 10000f) + (x * 1E-05f);
        this.draw(spriteBatch, x, y, draw_layer, alpha);
    }

    /// <inheritdoc />
    public override void draw(SpriteBatch spriteBatch, int xNonTile, int yNonTile, float layerDepth, float alpha = 1)
    {
        if (this.sourceRect == default)
        {
            int season = GetSeason(Game1.currentLocation);
            this.sourceRect = this.GetSourceRectForSeason(season);
        }

        if (this.sourceRect != default)
        {
            Vector2 position = Game1.GlobalToLocal(Game1.viewport, new Vector2(xNonTile * 64, (yNonTile * 64) - (this.sourceRect.Height * 4) + 64));
            spriteBatch.Draw(
                texture: Bush.texture.Value,
                position,
                sourceRectangle: this.sourceRect,
                color: Color.White * alpha,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: Vector2.One * Game1.pixelZoom,
                effects: SpriteEffects.None,
                layerDepth);
        }
    }

    /// <inheritdoc />
    public override void drawPlacementBounds(SpriteBatch spriteBatch, GameLocation location)
    {
        int X = (int)Game1.GetPlacementGrabTile().X * 64;
        int Y = (int)Game1.GetPlacementGrabTile().Y * 64;
        Game1.isCheckingNonMousePlacement = !Game1.IsPerformingMousePlacement();
        if (Game1.isCheckingNonMousePlacement)
        {
            Vector2 nearbyValidPlacementPosition = Utility.GetNearbyValidPlacementPosition(Game1.player, location, this, X, Y);
            X = (int)nearbyValidPlacementPosition.X;
            Y = (int)nearbyValidPlacementPosition.Y;
        }

        int width = ((BushSizes)this.ParentSheetIndex).GetWidth();
        bool canPlaceHere = Utility.playerCanPlaceItemHere(location, this, X, Y, Game1.player) && Utility.withinRadiusOfPlayer(X, Y, 1, Game1.player);
        for (int x_offset = 0; x_offset < width; x_offset++)
        {
            spriteBatch.Draw(
                texture: Game1.mouseCursors,
                new Vector2((((X / 64) + x_offset) * 64) - Game1.viewport.X, Y - Game1.viewport.Y),
                new Rectangle(canPlaceHere ? 194 : 210, 388, 16, 16),
                color: Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 4f,
                effects: SpriteEffects.None,
                layerDepth: 0.01f);
        }
        this.draw(spriteBatch, X / 64, Y / 64, 0.5f);
    }

    /// <inheritdoc />
    public override void drawAsProp(SpriteBatch b)
    {
        this.draw(b, (int)this.TileLocation.X, (int)this.TileLocation.Y);
    }

    public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
    {
        if (this.sourceRect == default)
        {
            int season = GetSeason(Game1.currentLocation);
            this.sourceRect = this.GetSourceRectForSeason(season);
        }

        if (this.sourceRect != default)
        {
            spriteBatch.Draw(
                Bush.texture.Value,
                location + new Vector2(this.sourceRect.Width < this.sourceRect.Height && (BushSizes)this.ParentSheetIndex != BushSizes.Medium ? 32f : 16f, 32f),
                this.sourceRect,
                color * transparency,
                0f,
                new Vector2(8f, 16f),
                this.getScaleSize() * scaleSize,
                SpriteEffects.None,
                layerDepth);
            if (((drawStackNumber == StackDrawType.Draw && this.maximumStackSize() > 1 && this.Stack > 1) || drawStackNumber == StackDrawType.Draw_OneInclusive)
                && scaleSize > 0.3f && this.Stack != int.MaxValue)
            {
                Utility.drawTinyDigits(
                    toDraw: this.stack.Value,
                    b: spriteBatch,
                    position: location + new Vector2((float)(64 - Utility.getWidthOfTinyDigitString(this.stack, 3f * scaleSize)) + 3f * scaleSize, 64f - 18f * scaleSize + 2f),
                    scale: 3f * scaleSize,
                    layerDepth: 1f,
                    c: Color.White);
            }
        }
    }

    private float getScaleSize() =>
        (BushSizes)this.ParentSheetIndex switch
        {
            BushSizes.Large or BushSizes.TownLarge=> 1f,
            BushSizes.Medium => 1.4f,
            _ => 2f
        };

    /// <inheritdoc />
    public override void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
    {
        if (this.sourceRect == default)
        {
            int season = GetSeason(f.currentLocation);
            this.sourceRect = this.GetSourceRectForSeason(season);
        }
        if (this.sourceRect != default)
        {
            int xOffset = (this.sourceRect.Width - 16) * 2;
            objectPosition.X -= xOffset;
            int yOffset = Math.Max(this.sourceRect.Height - 32, 0);
            objectPosition.Y -= yOffset;
            spriteBatch.Draw(
                texture: Bush.texture.Value,
                position: objectPosition,
                sourceRectangle: this.sourceRect,
                color: Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 4f,
                effects: SpriteEffects.None,
                layerDepth: Math.Max(0f, (f.getStandingY() + 3) / 10000f));
        }
    }

    #endregion

    #region misc

    public override Item getOne()
    {
        InventoryBush bush = new((BushSizes)this.ParentSheetIndex, 1);
        bush._GetOneFrom(this);
        return bush;
    }

    /// <inheritdoc />
    public override bool canBeShipped() => false;

    /// <inheritdoc />
    public override bool canBeGivenAsGift() => false;

    /// <inheritdoc />
    public override bool canBeTrashed() => true;

    /// <inheritdoc />
    public override string getCategoryName() => I18n.Category();

    /// <inheritdoc />
    public override Color getCategoryColor() => Color.Green;

    /// <inheritdoc />
    public override bool isPlaceable() => true;

    /// <inheritdoc />
    public override bool canBePlacedInWater() => false;

    /// <inheritdoc />
    public override bool canStackWith(ISalable other)
    {
        if (other is not InventoryBush otherBush)
        {
            return false;
        }
        return this.ParentSheetIndex == otherBush.ParentSheetIndex;
    }

    /// <inheritdoc />
    protected override string loadDisplayName() => (BushSizes)this.ParentSheetIndex switch
    {
        BushSizes.Small => I18n.Bush_Small(),
        BushSizes.Medium => I18n.Bush_Medium(),
        BushSizes.Large => I18n.Bush_Large(),
        BushSizes.SmallAlt => I18n.Bush_Small_Alt(),
        BushSizes.Town => I18n.Bush_Town(),
        BushSizes.TownLarge => I18n.Bush_Town_Big(),
        BushSizes.Walnut => I18n.Bush_Walnut(),
        BushSizes.Harvested => I18n.Bush_Harvested(),
        _ => "Error Bush",
    };

    /// <inheritdoc />
    public override string getDescription() => (BushSizes)this.ParentSheetIndex switch
    {
        BushSizes.Small => I18n.Bush_Small_Description(),
        BushSizes.Medium => I18n.Bush_Medium_Description(),
        BushSizes.Large => I18n.Bush_Large_Description(),
        BushSizes.SmallAlt => I18n.Bush_Small_Alt_Description(),
        BushSizes.Town => I18n.Bush_Town_Description(),
        BushSizes.TownLarge => I18n.Bush_Town_Big_Description(),
        BushSizes.Walnut => I18n.Bush_Walnut_Description(),
        BushSizes.Harvested => I18n.Bush_Harvested_Description(),
        _ => "This should have not have happened. What. You should probably just trash this."
    };

    /// <inheritdoc />
    protected override void _PopulateContextTags(HashSet<string> tags)
    {
        tags.Add("category_inventory_bush");
        tags.Add($"id_inventoryBush_{this.ParentSheetIndex}");
        tags.Add("quality_none");
        tags.Add("item_" + this.SanitizeContextTag(this.Name));
    }

    #endregion

    #region helpers

    internal void UpdateForNewLocation(GameLocation location)
    {
        int season = GetSeason(location);
        this.sourceRect = this.GetSourceRectForSeason(season);
    }

    private static bool IsTilePlaceableForBush(GameLocation location, int tileX, int tileY)
    {
        if (location is null)
        {
            return false;
        }

        if (location.doesTileHaveProperty(tileX, tileY, "Water", "Back") is not null)
        {
            return false;
        }

        foreach (Farmer farmer in location.farmers)
        {
            if (farmer.GetBoundingBox().Intersects(new Rectangle(tileX * 64, tileY * 64, 64, 64)))
            {
                return false;
            }
        }

        Vector2 tile = new(tileX, tileY);
        return !location.isTileOccupied(tile);
    }

    private static int GetSeason(GameLocation loc)
        => Utility.getSeasonNumber(Game1.GetSeasonForLocation(loc));

    // derived from Bush.setUpSourceRect
    private Rectangle GetSourceRectForSeason(int season)
    {
        switch ((BushSizes)this.ParentSheetIndex)
        {
            case BushSizes.Small:
                return new Rectangle(season * 32, 224, 16, 32);
            case BushSizes.SmallAlt:
                return new Rectangle((season * 32) + 16, 224, 16, 32);
            case BushSizes.Medium:
                int y = Math.DivRem(season * 64, Bush.texture.Value.Bounds.Width, out int x);
                return new Rectangle(x, y, 32, 48);
            case BushSizes.Town:
                return new Rectangle(season * 32, 96, 32, 32);
            case BushSizes.Large:
                return season switch
                {
                    0 or 1 => new Rectangle(0, 128, 48, 48),
                    2 => new Rectangle(48, 128, 48, 48),
                    _ => new Rectangle(0, 176, 48, 48),
                };
            case BushSizes.TownLarge:
                return season switch
                {
                    0 or 1 => new Rectangle(48, 176, 48, 48),
                    2 => new Rectangle(48, 128, 48, 48),
                    _ => new Rectangle(0, 176, 48, 48),
                };
            case BushSizes.Harvested:
                return new Rectangle(0, 320, 32, 32);
            case BushSizes.Walnut:
                return new Rectangle(32, 320, 32, 32);
            default:
                return default;
        }
    }
    #endregion
}