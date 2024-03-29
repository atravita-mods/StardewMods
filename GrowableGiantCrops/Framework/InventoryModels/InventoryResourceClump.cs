﻿using System.Xml.Serialization;

using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.Framework.InventoryModels;

/// <summary>
/// A giant clump in the inventory.
/// </summary>
[XmlType("Mods_atravita_InventoryResourceClump")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Keeping like methods together.")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Keeping like elements together.")]
public sealed class InventoryResourceClump : SObject
{
    [XmlIgnore]
    private Rectangle sourceRect = default;

    /// <summary>
    /// A prefix to the name of inventory resource clumps.
    /// </summary>
    internal const string ResourcePrefix = "atravita.ResourceClump.";

    /// <summary>
    /// The moddata string used to identify placed inventory resource clumps.
    /// </summary>
    internal const string ResourceModdata = $"{ResourcePrefix}Type";

    /// <summary>
    /// Numeric category ID used to identify JA/vanilla giant crops.
    /// </summary>
    internal const int ResourceClump = -15576655; // set a large random negative number

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryResourceClump"/> class.
    /// This constructor is for the serializer. Do not use it.
    /// </summary>
    public InventoryResourceClump()
        : base()
    {
        this.Edibility = inedible;
        this.Price = 0;
        this.Category = ResourceClump;
        this.bigCraftable.Value = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryResourceClump"/> class.
    /// </summary>
    /// <param name="idx">the resource clump index.</param>
    /// <param name="initialStack">the initial stack size.</param>
    public InventoryResourceClump(ResourceClumpIndexes idx, int initialStack)
        : base((int)idx, initialStack, false, -1, 0)
    {
        if (!ResourceClumpIndexesExtensions.IsDefined(idx))
        {
            ModEntry.ModMonitor.Log($"Resource clump {idx.ToStringFast()} doesn't seem to be a valid resource clump. Setting to stump.", LogLevel.Error);
            this.ParentSheetIndex = (int)ResourceClumpIndexes.Stump;
        }

        this.CanBeSetDown = true;
        this.Name = ResourcePrefix + ((ResourceClumpIndexes)this.ParentSheetIndex).ToStringFast();
        this.Edibility = inedible;
        this.Price = 0;
        this.Category = ResourceClump;
        this.bigCraftable.Value = true;
    }

    /// <summary>
    /// Gets the source rectangle to draw (from Maps/SpringObjects).
    /// </summary>
    [XmlIgnore]
    internal Rectangle SourceRect
    {
        get
        {
            if (this.sourceRect == default)
            {
                this.sourceRect = GetSourceRect(this.ParentSheetIndex);
            }
            return this.sourceRect;
        }
    }

    #region reflection
    private static readonly Action<ResourceClump, float> ShakeTimerSetter = typeof(ResourceClump)
        .GetCachedField("shakeTimer", ReflectionCache.FlagTypes.InstanceFlags)
        .GetInstanceFieldSetter<ResourceClump, float>();

    /// <summary>
    /// Shakes a resource clump.
    /// </summary>
    /// <param name="clump">The clump to shake.</param>
    internal static void ShakeResourceClump(ResourceClump clump)
    {
        ShakeTimerSetter(clump, 500f);
        clump.NeedsUpdate = true;
    }
    #endregion

    #region placement

    /// <inheritdoc />
    public override bool canBePlacedHere(GameLocation l, Vector2 tile)
        => this.CanPlace(l, tile, ModEntry.Config.RelaxedPlacement);

    /// <summary>
    /// Checks to see if a resource clump can be placed.
    /// </summary>
    /// <param name="l">Game location.</param>
    /// <param name="tile">tile to place on.</param>
    /// <param name="relaxed">Whether or not to use relaxed placement rules.</param>
    /// <returns>True to allow placement, false otherwise.</returns>
    internal bool CanPlace(GameLocation l, Vector2 tile, bool relaxed)
    {
        if (l.resourceClumps is null || Utility.isPlacementForbiddenHere(l))
        {
            return false;
        }

        ResourceClumpIndexes size = (ResourceClumpIndexes)this.ParentSheetIndex;
        if (size == ResourceClumpIndexes.Invalid || !ResourceClumpIndexesExtensions.IsDefined(size))
        {
            return false;
        }

        for (int x = (int)tile.X; x < (int)tile.X + 2; x++)
        {
            for (int y = (int)tile.Y - 1; y < (int)tile.Y + 1; y++)
            {
                if (!GGCUtils.IsTilePlaceableForResourceClump(l, x, y, relaxed))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool placementAction(GameLocation location, int x, int y, Farmer? who = null)
        => this.PlaceResourceClump(location, x, y, ModEntry.Config.RelaxedPlacement);

    /// <summary>
    /// Places a clump.
    /// </summary>
    /// <param name="location">Location to place in.</param>
    /// <param name="x">pixel coordinate to place, x.</param>
    /// <param name="y">pixel coordinate to place, y.</param>
    /// <param name="relaxed">Whether or not to use relaxed placement rules.</param>
    /// <returns>True if placed, false otherwise.</returns>
    internal bool PlaceResourceClump(GameLocation location, int x, int y, bool relaxed)
    {
        if (location.resourceClumps is null || Utility.isPlacementForbiddenHere(location))
        {
            return false;
        }

        ResourceClumpIndexes size = (ResourceClumpIndexes)this.ParentSheetIndex;
        if (size == ResourceClumpIndexes.Invalid || !ResourceClumpIndexesExtensions.IsDefined(size))
        {
            return false;
        }

        Vector2 placementTile = new(x / Game1.tileSize, y / Game1.tileSize);
        if (!this.CanPlace(location, placementTile, relaxed))
        {
            return false;
        }

        // HACK: the game wants to spawn from the top left corner, but placement is usually done on the lower
        // edge, so just subtract here.
        placementTile.Y -= 1;
        ResourceClump clump = new(this.ParentSheetIndex, 2, 2, placementTile);
        if (ModEntry.Config.PreserveModData)
        {
            clump.modData.CopyModDataFrom(this.modData);
        }
        clump.modData?.SetEnum(ResourceModdata, size);
        location.resourceClumps.Add(clump);
        location.playSound("thudStep");
        ShakeResourceClump(clump);
        return true;
    }

    #endregion

    #region draw

    /// <inheritdoc />
    public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1)
    {
        float draw_layer = Math.Max(
            0f,
            ((y * Game1.tileSize) + 40) / 10000f) + (x * 1E-05f);
        this.draw(spriteBatch, x, y, draw_layer, alpha);
    }

    /// <inheritdoc />
    public override void draw(SpriteBatch spriteBatch, int xNonTile, int yNonTile, float layerDepth, float alpha = 1)
    {
        if (this.sourceRect == default)
        {
            this.sourceRect = GetSourceRect(this.ParentSheetIndex);
        }

        if (this.sourceRect != default)
        {
            Vector2 position = Game1.GlobalToLocal(
                viewport: Game1.viewport,
                new Vector2(xNonTile * Game1.tileSize, (yNonTile * Game1.tileSize) - (this.sourceRect.Height * Game1.pixelZoom) + Game1.tileSize));
            spriteBatch.Draw(
                texture: Game1.objectSpriteSheet,
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
        int x = (int)Game1.GetPlacementGrabTile().X * Game1.tileSize;
        int y = (int)Game1.GetPlacementGrabTile().Y * Game1.tileSize;
        Game1.isCheckingNonMousePlacement = !Game1.IsPerformingMousePlacement();
        if (Game1.isCheckingNonMousePlacement)
        {
            Vector2 nearbyValidPlacementPosition = Utility.GetNearbyValidPlacementPosition(Game1.player, location, this, x, y);
            x = (int)nearbyValidPlacementPosition.X;
            y = (int)nearbyValidPlacementPosition.Y;
        }

        bool canPlaceHere = Utility.playerCanPlaceItemHere(location, this, x, y, Game1.player)
                                && Utility.withinRadiusOfPlayer(x, y, 1, Game1.player);
        for (int x_offset = 0; x_offset < 2; x_offset++)
        {
            for (int y_offset = -1; y_offset < 1; y_offset++)
            {
                spriteBatch.Draw(
                    texture: Game1.mouseCursors,
                    new Vector2(x + (x_offset * Game1.tileSize) - Game1.viewport.X, y + (y_offset * Game1.tileSize) - Game1.viewport.Y),
                    new Rectangle(canPlaceHere ? 194 : 210, 388, 16, 16),
                    color: Color.White,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale: 4f,
                    effects: SpriteEffects.None,
                    layerDepth: 0.01f);
            }
        }
        this.draw(spriteBatch, x / Game1.tileSize, y / Game1.tileSize, 0.5f);
    }

    /// <inheritdoc />
    public override void drawAsProp(SpriteBatch b)
    {
        this.draw(b, (int)this.TileLocation.X, (int)this.TileLocation.Y);
    }

    /// <inheritdoc />
    public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
    {
        if (this.sourceRect == default)
        {
            this.sourceRect = GetSourceRect(this.ParentSheetIndex);
        }

        if (this.sourceRect != default)
        {
            spriteBatch.Draw(
                Game1.objectSpriteSheet,
                location + new Vector2(20f, 32f),
                this.sourceRect,
                color * transparency,
                0f,
                new Vector2(8f, 16f),
                this.GetScaleSize() * scaleSize,
                SpriteEffects.None,
                layerDepth);
            if (((drawStackNumber == StackDrawType.Draw && this.maximumStackSize() > 1 && this.Stack > 1) || drawStackNumber == StackDrawType.Draw_OneInclusive)
                && scaleSize > 0.3f && this.Stack != int.MaxValue)
            {
                Utility.drawTinyDigits(
                    toDraw: this.Stack,
                    b: spriteBatch,
                    position: location + new Vector2(Game1.tileSize - Utility.getWidthOfTinyDigitString(this.Stack, 3f * scaleSize) + (3f * scaleSize), 64f - (18f * scaleSize) + 2f),
                    scale: 3f * scaleSize,
                    layerDepth: 1f,
                    c: Color.White);
            }
        }
    }

    /// <inheritdoc />
    public override void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
    {
        if (this.sourceRect == default)
        {
            this.sourceRect = GetSourceRect(this.ParentSheetIndex);
        }
        if (this.sourceRect != default)
        {
            int xOffset = (this.sourceRect.Width - 16) * 2;
            objectPosition.X -= xOffset;
            int yOffset = Math.Max((this.sourceRect.Height * Game1.pixelZoom) - (2 * Game1.tileSize), 0);
            objectPosition.Y -= yOffset;
            spriteBatch.Draw(
                texture: Game1.objectSpriteSheet,
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

    private float GetScaleSize() => 1.6f;

    #endregion

    #region misc

    /// <inheritdoc />
    public override Item getOne()
    {
        InventoryResourceClump clump = new((ResourceClumpIndexes)this.ParentSheetIndex, 1);
        clump._GetOneFrom(this);
        return clump;
    }

    /// <inheritdoc />
    public override int maximumStackSize() => ModEntry.Config.AllowLargeItemStacking ? 999 : 1;

    /// <inheritdoc />
    public override bool canBeShipped() => false;

    /// <inheritdoc />
    public override bool canBeGivenAsGift() => false;

    /// <inheritdoc />
    public override bool canBeTrashed() => true;

    /// <inheritdoc />
    public override string getCategoryName() => I18n.ResourceClumpCategory();

    /// <inheritdoc />
    public override Color getCategoryColor() => Color.SlateGray;

    /// <inheritdoc />
    public override bool isPlaceable() => true;

    /// <inheritdoc />
    public override bool isForage(GameLocation location) => false;

    /// <inheritdoc />
    public override bool canBePlacedInWater() => false;

    /// <inheritdoc />
    public override bool canStackWith(ISalable other)
    {
        if (other is not InventoryResourceClump otherClump)
        {
            return false;
        }
        return this.ParentSheetIndex == otherClump.ParentSheetIndex
            && (!ModEntry.Config.PreserveModData || this.modData.ModDataMatches(otherClump.modData));
    }

    /// <inheritdoc/>
    protected override string loadDisplayName()
        => (ResourceClumpIndexes)this.ParentSheetIndex switch
        {
            ResourceClumpIndexes.Stump => I18n.Stump_Name(),
            ResourceClumpIndexes.HollowLog => I18n.HollowLog_Name(),
            ResourceClumpIndexes.Meteorite => I18n.Meteorite_Name(),
            ResourceClumpIndexes.Boulder => I18n.Boulder_Name(),
            ResourceClumpIndexes.MineRockOne or ResourceClumpIndexes.MineRockTwo => I18n.MineRockOne_Name(),
            ResourceClumpIndexes.MineRockThree or ResourceClumpIndexes.MineRockFour => I18n.MineRockOne_Name(),
            _ => I18n.ResourceClumpInvalid_Name(),
        };

    /// <inheritdoc/>
    public override string getDescription()
        => (ResourceClumpIndexes)this.ParentSheetIndex switch
        {
            ResourceClumpIndexes.Stump => I18n.Stump_Description(),
            ResourceClumpIndexes.HollowLog => I18n.HollowLog_Description(),
            ResourceClumpIndexes.Meteorite => I18n.Meteorite_Description(),
            ResourceClumpIndexes.Boulder => I18n.Boulder_Description(),
            ResourceClumpIndexes.MineRockOne or ResourceClumpIndexes.MineRockTwo => I18n.MineRockOne_Description(),
            ResourceClumpIndexes.MineRockThree or ResourceClumpIndexes.MineRockFour => I18n.MineRockOne_Description(),
            _ => I18n.ResourceClumpInvalid_Description(),
        };

    /// <inheritdoc />
    protected override void _PopulateContextTags(HashSet<string> tags)
    {
        tags.Add("category_inventory_resource_clump");
        tags.Add($"id_inventoryResourceClump_{this.ParentSheetIndex}");
        tags.Add("quality_none");
        tags.Add("item_" + this.SanitizeContextTag(this.Name));
    }

    #endregion

    #region helpers

    /// <summary>
    /// Gets the index associated with a specific clump.
    /// </summary>
    /// <param name="clump">Clump to get the index for.</param>
    /// <returns><see cref="ResourceClumpIndexes"/>, or <see cref="ResourceClumpIndexes.Invalid"/> if it could not be matched up.</returns>
    internal static ResourceClumpIndexes GetMatchingClumpIndex(ResourceClump clump)
    {
        ResourceClumpIndexes idx = (ResourceClumpIndexes)clump.parentSheetIndex.Value;
        return ResourceClumpIndexesExtensions.IsDefined(idx) ? idx : ResourceClumpIndexes.Invalid;
    }

    /// <summary>
    /// Checks to see if an int index is a valid index for a resource clump.
    /// </summary>
    /// <param name="index">the int index.</param>
    /// <returns>True if we recognize it, false otherwise.</returns>
    internal static bool IsValidClumpIndex(int index)
    {
        ResourceClumpIndexes idx = (ResourceClumpIndexes)index;
        return idx != ResourceClumpIndexes.Invalid && ResourceClumpIndexesExtensions.IsDefined(idx);
    }

    // derived from ResourceClump.draw
    private static Rectangle GetSourceRect(int idx)
    {
        Rectangle sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, idx, 16, 16);
        sourceRect.Width = 32;
        sourceRect.Height = 32;

        return sourceRect;
    }
    #endregion
}
