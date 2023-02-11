using System.Xml.Serialization;

using AtraCore.Utilities;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.Shims;
using GrowableGiantCrops.Framework.Assets;
using GrowableGiantCrops.Framework.InventoryModels;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

using XLocation = xTile.Dimensions.Location;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// A shovel.
/// </summary>
[XmlType("Mods_atravita_Shovel")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Like methods are grouped together.")]
public sealed class ShovelTool : GenericTool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShovelTool"/> class.
    /// </summary>
    public ShovelTool()
        : base(I18n.Shovel_Name(), I18n.Shovel_Description(), 0, 0, 0)
    {
        this.Stackable = false;
    }

    /// <inheritdoc />
    public override Item getOne()
    {
        ShovelTool newShovel = new();
        newShovel._GetOneFrom(this);
        return newShovel;
    }

    #region functionality

    /// <inheritdoc />
    public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
    {
        // use the watering can arms.
        who.jitterStrength = 0.25f;
        switch (who.FacingDirection)
        {
            case Game1.up:
                who.FarmerSprite.setCurrentFrame(180);
                break;
            case Game1.right:
                who.FarmerSprite.setCurrentFrame(172);
                break;
            case Game1.down:
                who.FarmerSprite.setCurrentFrame(164);
                break;
            case Game1.left:
                who.FarmerSprite.setCurrentFrame(188);
                break;
        }
        this.Update(who.FacingDirection, 0, who);
        return false;
    }

    /// <inheritdoc />
    public override void endUsing(GameLocation location, Farmer who)
    {
        who.stopJittering();
        who.canReleaseTool = false;

        // use the watering can arms.
        switch (who.FacingDirection)
        {
            case 2:
                ((FarmerSprite)who.Sprite).animateOnce(164, 125f, 3);
                break;
            case 1:
                ((FarmerSprite)who.Sprite).animateOnce(172, 125f, 3);
                break;
            case 0:
                ((FarmerSprite)who.Sprite).animateOnce(180, 125f, 3);
                break;
            case 3:
                ((FarmerSprite)who.Sprite).animateOnce(188, 125f, 3);
                break;
        }
    }

    /// <summary>
    /// Does the actual tool function.
    /// </summary>
    /// <param name="location">The game location.</param>
    /// <param name="x">pixel x.</param>
    /// <param name="y">pixel y.</param>
    /// <param name="power">The power level of the tool</param>
    /// <param name="who">Last farmer to use.</param>
    public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
    {
        try
        {
            base.DoFunction(location, x, y, power, who);
            Vector2 pickupTile = new(x / Game1.tileSize, y / Game1.tileSize);

            location.performToolAction(this, x / Game1.tileSize, y / Game1.tileSize);

            // Handle bushes.
            if (ModEntry.GrowableBushesAPI?.TryPickUpBush(location, pickupTile) is SObject bush)
            {
                ModEntry.ModMonitor.DebugOnlyLog($"Picking up bush {bush.Name}", LogLevel.Info);
                GiveItemOrMakeDebris(location, who, bush);
                ModEntry.GrowableBushesAPI.DrawPickUpGraphics(bush, location, bush.TileLocation);
                who.Stamina -= ModEntry.Config.ShovelEnergy;
                return;
            }

            // Handle normal game resource clumps.
            for (int i = location.resourceClumps.Count - 1; i >= 0; i--)
            {
                ResourceClump? clump = location.resourceClumps[i];
                if (clump is null || !clump.getBoundingBox(clump.tile.Value).Contains(x, y))
                {
                    continue;
                }
                if (GetMatchingInventoryItem(location, clump) is SObject item)
                {
                    ModEntry.ModMonitor.DebugOnlyLog($"Picking up {item.Name}", LogLevel.Info);
                    GiveItemOrMakeDebris(location, who, item);
                    who.Stamina -= ModEntry.Config.ShovelEnergy;
                    location.resourceClumps[i].performToolAction(this, 0, pickupTile, location);
                    location.resourceClumps.RemoveAt(i);
                    return;
                }
            }

            // handle secret woods clumps
            if (location is Woods woods)
            {
                for (int i = woods.stumps.Count - 1; i >= 0; i--)
                {
                    ResourceClump? clump = woods.stumps[i];
                    if (clump is null || !clump.getBoundingBox(clump.tile.Value).Contains(x, y))
                    {
                        continue;
                    }
                    if (GetMatchingInventoryItem(woods, clump) is SObject item)
                    {
                        ModEntry.ModMonitor.DebugOnlyLog($"Picking up {item.Name}", LogLevel.Info);
                        GiveItemOrMakeDebris(woods, who, item);
                        who.Stamina -= ModEntry.Config.ShovelEnergy;
                        woods.stumps[i].performToolAction(this, 0, pickupTile, woods);
                        woods.stumps.RemoveAt(i);
                        return;
                    }
                }
            }

            // the log blocking off the secret forest.
            if (location is Forest forest)
            {
                if (forest.log is not null && forest.log.getBoundingBox(forest.log.tile.Value).Contains(x, y))
                {
                    if (GetMatchingInventoryItem(forest, forest.log) is SObject item)
                    {
                        ModEntry.ModMonitor.DebugOnlyLog($"Picking up {item.Name}", LogLevel.Info);
                        GiveItemOrMakeDebris(forest, who, item);
                        who.Stamina -= ModEntry.Config.ShovelEnergy;
                        forest.log.performToolAction(this, 0, pickupTile, forest);
                        forest.log = null;
                        return;
                    }
                }
            }

            // handle FTM resource clumps.
            if (FarmTypeManagerShims.GetEmbeddedResourceClump is not null)
            {
                for (int i = location.largeTerrainFeatures.Count - 1; i >= 0; i--)
                {
                    ResourceClump? clump = FarmTypeManagerShims.GetEmbeddedResourceClump(location.largeTerrainFeatures[i]);
                    if (clump is null || !clump.getBoundingBox(clump.tile.Value).Contains(x, y))
                    {
                        continue;
                    }
                    if (GetMatchingInventoryItem(location, clump) is SObject item)
                    {
                        ModEntry.ModMonitor.DebugOnlyLog($"Picking up {item.Name}", LogLevel.Info);
                        GiveItemOrMakeDebris(location, who, item);
                        who.Stamina -= ModEntry.Config.ShovelEnergy;
                        location.largeTerrainFeatures[i].performToolAction(this, 0, pickupTile, location);
                        location.largeTerrainFeatures.RemoveAt(i);
                        return;
                    }
                }
            }

            // for small things we take only one energy, at most.
            int energy = Math.Min(ModEntry.Config.ShovelEnergy, 1);
            if (location.terrainFeatures.TryGetValue(pickupTile, out TerrainFeature? terrain))
            {
                if (terrain.performToolAction(this, 0, pickupTile, location))
                {
                    who.Stamina -= energy;
                    location.terrainFeatures.Remove(pickupTile);
                    return;
                }
            }

            if (location.objects.TryGetValue(pickupTile, out SObject? obj))
            {
                // TODO: consider moving slime balls? "Slime Ball"

                // special case: shovel pushes full chests.
                if (obj is Chest chest && !chest.isEmpty())
                {
                    chest.GetMutex().RequestLock(
                        acquired: () =>
                        {
                             location.playSound("hammer");
                             chest.shakeTimer = 100;
                             if (chest.TileLocation.X == 0f && chest.TileLocation.Y == 0f && location.getObjectAtTile((int)pickupTile.X, (int)pickupTile.Y) == chest)
                             {
                                   chest.TileLocation = pickupTile;
                             }
                             chest.MoveToSafePosition(location, chest.TileLocation, 0, who.GetFacingDirection());
                             who.Stamina -= energy;
                             return;
                        },
                        failed: () => ModEntry.ModMonitor.Log($"Chest at {chest.TileLocation}: lock not acquired, skipping"));
                    return;
                }

                if (obj.bigCraftable.Value && obj.GetType() == typeof(SObject))
                {
                    if (obj.Name == "Mushroom Box")
                    {
                        who.Stamina -= energy;
                        obj.ParentSheetIndex = 128;
                        if (obj.readyForHarvest.Value)
                        {
                            location.debris.Add(new Debris(obj.heldObject.Value, who.Position));
                            obj.heldObject.Value = null;
                        }
                        obj.performRemoveAction(pickupTile, location);
                        GiveItemOrMakeDebris(location, who, obj);
                        location.objects.Remove(pickupTile);
                        return;
                    }
                }

                if (obj.performToolAction(this, location))
                {
                    who.Stamina -= energy;
                    location.objects.Remove(pickupTile);
                    return;
                }
            }

            // derived from Hoe - this makes hoedirt.
            if (location.doesTileHaveProperty((int)pickupTile.X, (int)pickupTile.Y, "Diggable", "Back") is null
                || location.isTileOccupied(pickupTile) || !location.isTilePassable(new XLocation((int)pickupTile.X, (int)pickupTile.Y), Game1.viewport))
            {
                return;
            }

            who.Stamina -= energy;
            location.makeHoeDirt(pickupTile, ignoreChecks: false);
            location.playSound("hoeHit");
            Game1.removeSquareDebrisFromTile((int)pickupTile.X, (int)pickupTile.Y);
            location.checkForBuriedItem((int)pickupTile.X, (int)pickupTile.Y, explosion: false, detectOnly: false, who);
            MultiplayerHelpers.GetMultiplayer().broadcastSprites(location, new TemporaryAnimatedSprite(
                rowInAnimationTexture: 12,
                new Vector2(pickupTile.X * 64f, pickupTile.Y * 64f),
                color: Color.White,
                animationLength: 8,
                flipped: Game1.random.Next(2) == 0,
                animationInterval: 50f));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Unexpected error in using shovel:\n\n{ex}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Handles getting the matching inventory item.
    /// </summary>
    /// <param name="location">Game location we're at.</param>
    /// <param name="clump">The clump to check.</param>
    /// <returns>An SObject matching the inventory item variant, or null for not applicable.</returns>
    private static SObject? GetMatchingInventoryItem(GameLocation location, ResourceClump? clump)
    {
        switch (clump)
        {
            case GiantCrop giant:
            {
                InventoryGiantCrop? inventoryGiantCrop = null;
                if (giant.modData.TryGetValue(InventoryGiantCrop.GiantCropTweaksModDataKey, out string? stringID)
                    && ModEntry.GiantCropTweaksAPI?.GiantCrops.ContainsKey(stringID) == true)
                {
                    inventoryGiantCrop = new InventoryGiantCrop(stringID, giant.parentSheetIndex.Value, 1);
                }
                else if (InventoryGiantCrop.IsValidGiantCropIndex(giant.parentSheetIndex.Value))
                {
                    inventoryGiantCrop = new InventoryGiantCrop(giant.parentSheetIndex.Value, 1);
                }

                if (inventoryGiantCrop is not null)
                {
                    AddAnimations(location, giant.tile.Value, inventoryGiantCrop.TexturePath, inventoryGiantCrop.SourceRect, inventoryGiantCrop.TileSize);
                    return inventoryGiantCrop;
                }
                break;
            }
            case ResourceClump resource:
                ResourceClumpIndexes idx = (ResourceClumpIndexes)resource.parentSheetIndex.Value;
                if (idx != ResourceClumpIndexes.Invalid && ResourceClumpIndexesExtensions.IsDefined(idx))
                {
                    InventoryResourceClump inventoryResourceClump = new(idx, 1);
                    AddAnimations(location, resource.tile.Value, Game1.objectSpriteSheetName, inventoryResourceClump.SourceRect, new Point(2, 2));
                    return inventoryResourceClump;
                }
                break;
        }

        return null;
    }

    /// <inheritdoc />
    public override bool onRelease(GameLocation location, int x, int y, Farmer who) => false;

    #endregion

    #region display

    /// <inheritdoc />
    public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
    {
        spriteBatch.Draw(
            texture: AssetManager.ToolTexture,
            position: location + new Vector2(32f, 32f),
            new Rectangle(96, 16, 16, 16),
            color: color * transparency,
            rotation: 0f,
            new Vector2(8f, 8f),
            scale: 4f * scaleSize,
            effects: SpriteEffects.None,
            layerDepth);
    }

    /// <inheritdoc />
    protected override string loadDisplayName() => I18n.Shovel_Name();

    /// <inheritdoc />
    protected override string loadDescription() => I18n.Shovel_Description();

    #endregion

    #region misc

    /// <inheritdoc />
    /// <remarks>disallow forging.</remarks>
    public override bool CanForge(Item item) => false;

    /// <inheritdoc />
    /// <remarks>disallow stacking.</remarks>
    public override int maximumStackSize() => -1;

    /// <inheritdoc />
    /// <remarks>nop this.</remarks>
    public override void actionWhenClaimed()
    {
    }

    /// <inheritdoc />
    /// <remarks>forbid attachments.</remarks>
    public override int attachmentSlots() => 0;

    /// <inheritdoc />
    /// <remarks>forbid attachments.</remarks>
    public override bool canThisBeAttached(SObject o) => false;

    /// <inheritdoc />
    /// <remarks>forbid attachments.</remarks>
    public override SObject attach(SObject o) => o;

    #endregion

    #region helpers

    /// <summary>
    /// Helps animate a resource clump or large crop.
    /// </summary>
    /// <param name="loc">GameLocation.</param>
    /// <param name="tile">Tile to animate at.</param>
    /// <param name="texturePath">Path to the texture.</param>
    /// <param name="sourceRect">Sourcerect to use.</param>
    /// <param name="tileSize">The size of the item, in tiles.</param>
    internal static void AddAnimations(GameLocation loc, Vector2 tile, string? texturePath, Rectangle sourceRect, Point tileSize)
    {
        if (texturePath is null)
        {
            return;
        }

        Multiplayer mp = MultiplayerHelpers.GetMultiplayer();

        const float deltaY = -90;
        const float gravity = 0.0025f;

        float velocity = -0.7f - MathF.Sqrt(2 * 60f * gravity);
        float time = (MathF.Sqrt((velocity * velocity) - (gravity * deltaY * 2f)) / gravity) - (velocity / gravity);

        Vector2 landingPos = new Vector2(tile.X + (tileSize.X / 2f) - 1, tile.Y + tileSize.Y - 1) * 64f;

        TemporaryAnimatedSprite objTas = new(
            textureName: texturePath,
            sourceRect: sourceRect,
            position: tile * 64f,
            flipped: false,
            alphaFade: 0f,
            color: Color.White)
        {
            totalNumberOfLoops = 1,
            interval = time,
            acceleration = new Vector2(0f, gravity),
            motion = new Vector2(0f, velocity),
            scale = Game1.pixelZoom,
            timeBasedMotion = true,
            rotation = 0.1f,
            rotationChange = 0.1f,
            scaleChange = -0.0015f * (Math.Max(3, tileSize.Y) / 3),
            layerDepth = (landingPos.Y + 32f) / 10000f,
        };

        TemporaryAnimatedSprite? dustTas = new(
            textureName: Game1.mouseCursorsName,
            sourceRect: new Rectangle(464, 1792, 16, 16),
            animationInterval: 120f,
            animationLength: 5,
            numberOfLoops: 0,
            position: landingPos,
            flicker: false,
            flipped: Game1.random.NextDouble() < 0.5,
            layerDepth: (landingPos.Y + 40f) / 10000f,
            alphaFade: 0.01f,
            color: Color.White,
            scale: Game1.pixelZoom,
            scaleChange: 0.02f,
            rotation: 0f,
            rotationChange: 0f)
        {
            light = true,
            delayBeforeAnimationStart = Math.Max((int)time - 10, 0),
        };

        mp.broadcastSprites(loc, objTas, dustTas);
    }

    /// <summary>
    /// Tries to add an item to the player's inventory, dropping it at their feet if we can't.
    /// </summary>
    /// <param name="location">relevant location.</param>
    /// <param name="who">farmer to add to.</param>
    /// <param name="item">item to add.</param>
    private static void GiveItemOrMakeDebris(GameLocation location, Farmer who, Item item)
    {
        if (!who.addItemToInventoryBool(item))
        {
            location.debris.Add(new Debris(item, who.Position));
        }
    }

    #endregion
}
